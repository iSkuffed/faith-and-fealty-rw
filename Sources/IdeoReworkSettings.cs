using Verse;

namespace IdeoRework
{
    public enum CognitiveDissonanceMode
    {
        Legacy = 0,
        Experimental = 1,
        Disabled = 2
    }

    public class IdeoReworkSettings : ModSettings
    {
        public CognitiveDissonanceMode cognitiveDissonanceMode = CognitiveDissonanceMode.Legacy;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref cognitiveDissonanceMode, "cognitiveDissonanceMode", CognitiveDissonanceMode.Legacy);
            base.ExposeData();
        }
    }
}
