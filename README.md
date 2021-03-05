# Oracle Cache Provider for GMap.NET

## 介绍
使用 Oracle 数据库在 [GMap.NET](https://github.com/judero01col/GMap.NET) 中进行图块缓存

## 使用说明

```C#
// 1. 配置 Oracle 缓存实现对象
var cache = new OraclePureImageCache(oracleConnectionString, GMapImageProxy.Instance, "GMapNETcache");
// 2. 替换一级缓存实现（默认为 SQLite）
gMapControl.Manager.PrimaryCache = cache;
// or 附加二级缓存实现（默认为空）
gMapControl.Manager.SecondaryCache = cache;
```

> 说明：根据所用的桌面应用框架，向构造函数传入 GMap.NET.WindowsForms 或者 GMap.NET.WindowsPresentaion 中的 GMapImageProxy 对象。

## 依赖的 Nuget 包

* GMap.NET.Core [2.0.1,)

## 开发人员

* 秦文轩 (gitee.com/vxchin)
