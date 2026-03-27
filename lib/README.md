# Local dependency drop folder

Place the required game and modding assemblies in this folder before building:

- `BepInEx.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `Assembly-CSharp.dll`

Suggested sources:

- `BepInEx.dll`: from your ETG `BepInEx/core` directory
- `UnityEngine.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.CoreModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.IMGUIModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.TextRenderingModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `Assembly-CSharp.dll`: from `Enter the Gungeon\EtG_Data\Managed`

Do not commit these DLLs to the repository.
