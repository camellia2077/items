Release

python .\scripts\build.py --configuration Release
python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --configuration Release





如果你想立刻用最新功能，最简单的是继续部署 Debug，因为它已经是最新的：

python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon"

python .\scripts\deploy_mod.py "C:\Game\steam\steamapps\common\Enter the Gungeon" --overwrite-config





