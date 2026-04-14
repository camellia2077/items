# Local dependency drop folder

Place the required game and modding assemblies in this folder before building:

- `0Harmony.dll`
- `BepInEx.dll`
- `Ionic.Zip.dll`
- `ModTheGungeonAPI.dll`
- `Newtonsoft.Json.dll`
- `UnityEngine.CoreModule.MTGAPIPatcher.mm.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `Assembly-CSharp.dll`
- `System.Xml.dll`
- `System.Xml.Linq.dll`

Suggested sources:

- `0Harmony.dll`: from the `HarmonyX` package (`lib/net35/0Harmony.dll`) or an installed `ModTheGungeonAPI` dependency set
- `BepInEx.dll`: from your ETG `BepInEx/core` directory
- `Ionic.Zip.dll`: from the `Mod the Gungeon API` Thunderstore package (`plugins/MtGAPI/Ionic.Zip.dll`)
- `ModTheGungeonAPI.dll`: from the `Mod the Gungeon API` Thunderstore package (`plugins/MtGAPI/ModTheGungeonAPI.dll`)
- `Newtonsoft.Json.dll`: from the `Mod the Gungeon API` Thunderstore package (`plugins/MtGAPI/Newtonsoft.Json.dll`)
- `UnityEngine.CoreModule.MTGAPIPatcher.mm.dll`: from the `Mod the Gungeon API` Thunderstore package (`monomod/UnityEngine.CoreModule.MTGAPIPatcher.mm.dll`)
- `UnityEngine.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.CoreModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.IMGUIModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `UnityEngine.TextRenderingModule.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `Assembly-CSharp.dll`: from `Enter the Gungeon\EtG_Data\Managed`
- `System.Xml.dll`: from the `Mod the Gungeon API` Thunderstore package (`plugins/MtGAPI/System.Xml.dll`)
- `System.Xml.Linq.dll`: from the `Mod the Gungeon API` Thunderstore package (`plugins/MtGAPI/System.Xml.Linq.dll`)

Do not commit these DLLs to the repository.
