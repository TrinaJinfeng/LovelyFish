Write-Host "开始发布..."

# 1. 删除旧 publish
if (Test-Path .\publish) {
    Remove-Item .\publish -Recurse -Force
}

# 2. 发布项目
dotnet publish -c Release -o .\publish

Write-Host "发布完成，开始上传到 VPS..."

# 3. 上传到 VPS
scp -r .\publish\* root@192.46.221.104:/var/www/lovelyfish-backend/

Write-Host "上传完成，重启服务..."

# 4. 重启 systemd 服务
ssh root@192.46.221.104 "systemctl restart lovelyfish"

Write-Host "部署完成！"