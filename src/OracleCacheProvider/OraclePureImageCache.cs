using System;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;

namespace GMap.NET.Caching.Oracle
{
    /// <summary>
    /// 适用于 Oracle 数据库的 <see cref="PureImage" /> 缓存。
    /// </summary>
    public class OraclePureImageCache : PureImageCache, IDisposable
    {
        private readonly PureImageProxy _imageProxy;
        private readonly string _tableName;
        private OracleCommand _cmdFetch;

        private OracleCommand _cmdInsert;
        private OracleConnection _cnGet;
        private OracleConnection _cnSet;
        private string _connectionString;

        private bool _initialized;

        /// <summary>
        /// 创建一个 <see cref="OraclePureImageCache" /> 对象。
        /// </summary>
        /// <param name="connectionString">Oracle 数据库连接字符串。</param>
        /// <param name="pureImageProxy">
        /// 创建 <see cref="PureImage" /> 对象的代理类。
        /// <remarks>
        /// 适用于 Windows Forms 和 WPF 的代理类 GMapImageProxy 分别位于 GMap.NET.WinForms 和 GMap.NET.WinPresentation 程序集中。
        /// </remarks>
        /// </param>
        /// <param name="tableName">用于图片缓存的表名称。</param>
        public OraclePureImageCache(
            string connectionString, PureImageProxy pureImageProxy, string tableName = "GMapNETCache")
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _imageProxy = pureImageProxy ?? throw new ArgumentNullException(nameof(pureImageProxy));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        public string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (_connectionString == value) return;
                _connectionString = value;
                if (!Initialized) return;

                Dispose();
                Initialize();
            }
        }

        /// <summary>
        /// is cache initialized
        /// </summary>
        public bool Initialized
        {
            get
            {
                lock (this)
                {
                    return _initialized;
                }
            }
            private set
            {
                lock (this)
                {
                    _initialized = value;
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (_cmdInsert)
            {
                _cmdInsert?.Dispose();
                _cmdInsert = null;

                _cnSet?.Dispose();
                _cnSet = null;
            }

            lock (_cmdFetch)
            {
                _cmdFetch?.Dispose();
                _cmdFetch = null;

                _cnGet?.Dispose();
                _cnGet = null;
            }

            Initialized = false;
        }

        #endregion

        /// <summary>
        /// initialize connection to server
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            lock (this)
            {
                if (Initialized) return true;

                #region prepare oracle & cache table

                try
                {
                    // different connections so the multi-thread inserts and selects don't collide on open readers.
                    _cnGet = new OracleConnection(_connectionString);
                    _cnGet.Open();
                    _cnSet = new OracleConnection(_connectionString);
                    _cnSet.Open();

                    bool tableExists;
                    using (var cmd =
                        new OracleCommand($"SELECT COUNT(1) FROM tab WHERE tname = '{_tableName.ToUpperInvariant()}'",
                            _cnGet))
                    {
                        var result = cmd.ExecuteScalar();
                        tableExists = Int32.TryParse(result.ToString(), out var tableCount) && tableCount != 0;
                    }

                    if (!tableExists)
                        using (var cmd = new OracleCommand(
                            $"CREATE TABLE {_tableName} ( \n"
                            + "   Type number NOT NULL, \n"
                            + "   Zoom number NOT NULL, \n"
                            + "   X    number NOT NULL, \n"
                            + "   Y    number NOT NULL, \n"
                            + "   Tile blob   NOT NULL, \n"
                            + $"   CONSTRAINT PK_{_tableName} PRIMARY KEY (Type, Zoom, X, Y) \n"
                            + ")",
                            _cnGet))
                        {
                            cmd.ExecuteNonQuery();
                        }

                    _cmdFetch =
                        new OracleCommand(
                            $"SELECT Tile FROM {_tableName} WHERE X=:x AND Y=:y AND Zoom=:zoom AND Type=:type",
                            _cnGet);
                    _cmdFetch.Parameters.Add(":x", OracleDbType.Int32);
                    _cmdFetch.Parameters.Add(":y", OracleDbType.Int32);
                    _cmdFetch.Parameters.Add(":zoom", OracleDbType.Int32);
                    _cmdFetch.Parameters.Add(":type", OracleDbType.Int32);
                    _cmdFetch.Prepare();

                    _cmdInsert =
                        new OracleCommand(
                            $"INSERT INTO {_tableName} ( X, Y, Zoom, Type, Tile ) VALUES ( :x, :y, :zoom, :type, :tile )",
                            _cnSet);
                    _cmdInsert.Parameters.Add(":x", OracleDbType.Int32);
                    _cmdInsert.Parameters.Add(":y", OracleDbType.Int32);
                    _cmdInsert.Parameters.Add(":zoom", OracleDbType.Int32);
                    _cmdInsert.Parameters.Add(":type", OracleDbType.Int32);
                    _cmdInsert.Parameters.Add(":tile", OracleDbType.Blob); //, calcmaximgsize);
                    //can't prepare insert because of the IMAGE field having a variable size.  Could set it to some 'maximum' size?

                    Initialized = true;
                }
                catch (Exception ex)
                {
                    _initialized = false;
                    Debug.WriteLine(ex.Message);
                }

                #endregion

                return Initialized;
            }
        }

        #region PureImageCache Members

        public bool PutImageToCache(byte[] tile, int type, GPoint pos, int zoom)
        {
            var ret = true;
            {
                if (Initialize())
                    try
                    {
                        lock (_cmdInsert)
                        {
                            _cmdInsert.Parameters[":x"].Value = pos.X;
                            _cmdInsert.Parameters[":y"].Value = pos.Y;
                            _cmdInsert.Parameters[":zoom"].Value = zoom;
                            _cmdInsert.Parameters[":type"].Value = type;
                            _cmdInsert.Parameters[":tile"].Value = tile;
                            _cmdInsert.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        ret = false;
                        Dispose();
                    }
            }
            return ret;
        }

        public PureImage GetImageFromCache(int type, GPoint pos, int zoom)
        {
            PureImage ret = null;
            {
                if (Initialize())
                    try
                    {
                        object odata;
                        lock (_cmdFetch)
                        {
                            _cmdFetch.Parameters[":x"].Value = pos.X;
                            _cmdFetch.Parameters[":y"].Value = pos.Y;
                            _cmdFetch.Parameters[":zoom"].Value = zoom;
                            _cmdFetch.Parameters[":type"].Value = type;
                            odata = _cmdFetch.ExecuteScalar();
                        }

                        if (odata != null && odata != DBNull.Value)
                        {
                            var tile = (byte[])odata;
                            if (tile.Length > 0) ret = _imageProxy.FromArray(tile);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        ret = null;
                        Dispose();
                    }
            }
            return ret;
        }

        /// <summary>
        /// NotImplemented
        /// </summary>
        /// <param name="date"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        int PureImageCache.DeleteOlderThan(DateTime date, int? type)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}