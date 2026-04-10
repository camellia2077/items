用什么命令可以打包？
09:39



直接用这条：

python .\tools\build_release_package.py
如果你想显式指定 Release，可以用：

python .\tools\build_release_package.py --configuration Release
如果你想覆盖包名里的版本号，可以用：

python .\tools\build_release_package.py --version 0.2.0
