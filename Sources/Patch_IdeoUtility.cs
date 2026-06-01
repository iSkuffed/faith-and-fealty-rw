using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace IdeoRework
{
    [HarmonyPatch(typeof(IdeoUtility))]
    [HarmonyPatch("Notify_HistoryEvent")]
    [HarmonyPatch(new Type[] { typeof(HistoryEvent), typeof(bool) })]
    public static class Patch_IdeoUtility_NotifyHistoryEvent
    {
        static void Postfix(HistoryEvent ev, bool canApplySelfTookThoughts)
        {
            try
            {
                // Skip during world gen — player faction doesn't exist yet
                if (Find.World == null) return;

                if (ev.args.TryGetArg(HistoryEventArgsNames.Doer, out Pawn doer))
                {
                    if (doer.IsFreeColonist)
                    {
                        var doerReligion = doer.GetReligionIdeo();
                        if (doerReligion != null)
                        {
                            doerReligion.Notify_MemberTookAction(ev, canApplySelfTookThoughts);
                        }
                    }

                    if (doer.IsCaravanMember())
                    {
                        var caravan = CaravanUtility.GetCaravan(doer);
                        if (caravan != null)
                        {
                            for (int i = 0; i < caravan.pawns.Count; i++)
                            {
                                var p = caravan.pawns[i];
                                if (p != doer && p.IsFreeColonist)
                                {
                                    var pReligion = p.GetReligionIdeo();
                                    if (pReligion != null)
                                        pReligion.Notify_MemberKnows(ev, p);
                                }
                            }
                        }
                    }
                    else if (doer.Spawned)
                    {
                        var allPawns = doer.Map.mapPawns.AllPawnsSpawned;
                        for (int i = 0; i < allPawns.Count; i++)
                        {
                            var p = allPawns[i];
                            if (p != doer && p.IsFreeColonist)
                            {
                                var pReligion = p.GetReligionIdeo();
                                if (pReligion != null)
                                    pReligion.Notify_MemberKnows(ev, p);
                            }
                        }
                    }
                }
                else
                {
                    var allColonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
                    for (int i = 0; i < allColonists.Count; i++)
                    {
                        var pawn = allColonists[i];
                        var pawnReligion = pawn.GetReligionIdeo();
                        if (pawnReligion != null)
                        {
                            pawnReligion.Notify_MemberKnows(ev, pawn);
                        }
                    }
                }

                // Notify religion ideo precepts about history events
                foreach (var religionIdeo in ReligionIdeoTracker.AllReligionIdeos())
                {
                    var precepts = religionIdeo.PreceptsListForReading;
                    for (int i = 0; i < precepts.Count; i++)
                    {
                        try
                        {
                            precepts[i].Notify_HistoryEvent(ev);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("[IdeoRework] Religion precept Notify_HistoryEvent: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] Notify_HistoryEvent religion: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(IdeoUtility))]
    [HarmonyPatch("DoerWillingToDo")]
    [HarmonyPatch(new Type[] { typeof(HistoryEvent) })]
    public static class Patch_IdeoUtility_DoerWillingToDo
    {
        static void Postfix(HistoryEvent ev, ref bool __result)
        {
            try
            {
                // Skip during world gen — player faction doesn't exist yet
                if (Find.World == null) return;
                if (!__result) return;

                if (!ev.args.TryGetArg(HistoryEventArgsNames.Doer, out Pawn doer)) return;

                var religionIdeo = doer.GetReligionIdeo();
                if (religionIdeo != null && !religionIdeo.MemberWillingToDo(ev))
                {
                    __result = false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[IdeoRework] DoerWillingToDo religion: " + ex.Message);
            }
        }
    }
}
