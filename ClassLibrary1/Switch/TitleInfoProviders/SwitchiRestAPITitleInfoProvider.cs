using RomManagerShared.Base;

namespace RomManagerShared.Switch
{
    public class SwitchiRestAPITitleInfoProvider : ITitleInfoProvider
    {
      
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Source { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public async Task<Rom> GetTitleInfo(Rom rom)
        {
            return null;
        }

        public Task LoadTitleDatabaseAsync()
        {
            throw new NotImplementedException();
        }
    }

}
