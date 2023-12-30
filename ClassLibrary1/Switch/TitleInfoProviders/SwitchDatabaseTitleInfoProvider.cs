using RomManagerShared;

namespace RomManagerShared.Switch.TitleInfoProviders
{
    public class SwitchDatabaseTitleInfoProvider : ITitleInfoProvider
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SwitchDatabaseTitleInfoProvider()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        public string Source { get; set; }

        // Add database-related fields or dependencies

        public async Task<IRom> GetTitleInfo(IRom rom)
        {
            return null;
        }

        public Task LoadTitleDatabaseAsync()
        {
            throw new NotImplementedException();
        }
    }

}
