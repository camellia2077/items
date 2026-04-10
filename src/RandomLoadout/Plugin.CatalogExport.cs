namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private void TryExportPickupCatalogOnce()
        {
            if (_hasExportedPickupCatalog || _pickupCatalogExporter == null)
            {
                return;
            }

            EtgPickupCatalogExportResult exportResult = _pickupCatalogExporter.Export(_pickupResolver);
            if (exportResult.Succeeded)
            {
                _hasExportedPickupCatalog = true;
                _lastPickupCatalogExportFailure = null;
                Logger.LogInfo(
                    RandomLoadoutLog.Init(
                        "Exported grantable pickup catalog to '" + exportResult.TextOutputPath + "', '" + exportResult.JsonOutputPath + "', '" + exportResult.GroupedJsonOutputPath + "', and '" + exportResult.RulePoolOutputPath + "' (" + exportResult.EntryCount + " entries)."));
                return;
            }

            if (!string.Equals(_lastPickupCatalogExportFailure, exportResult.FailureReason, System.StringComparison.Ordinal))
            {
                _lastPickupCatalogExportFailure = exportResult.FailureReason;
                Logger.LogWarning(RandomLoadoutLog.Init("Failed to export grantable pickup catalog: " + exportResult.FailureReason));
            }
        }
    }
}
