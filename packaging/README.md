# Windows 发布

## 生成自包含发布目录和 ZIP

在仓库根目录运行：

```powershell
.\packaging\publish-windows.ps1
```

默认生成 `win-x64` 自包含发布目录和 ZIP。需要 ARM64 时运行：

```powershell
.\packaging\publish-windows.ps1 -Runtime win-arm64
```

## 生成安装包

1. 先运行发布脚本。
2. 安装 [Inno Setup](https://jrsoftware.org/isinfo.php)。
3. 打开 `packaging/EyesGuard.iss` 编译安装包。

安装包输出到 `.artifacts/installer`，默认按当前脚本生成的 `win-x64` 内容打包。

## 自动更新清单

在设置页填写 HTTPS manifest 地址。格式参考 `update-manifest.example.json`：

```json
{
  "version": "0.1.1",
  "downloadUrl": "https://updates.example.com/EyesGuard-Setup-0.1.1.exe",
  "sha256": "...",
  "releaseNotes": "修复问题并改进稳定性"
}
```

下载后会校验 SHA-256，再启动安装程序。当前未配置服务器时，更新功能保持离线，不会发起请求。
