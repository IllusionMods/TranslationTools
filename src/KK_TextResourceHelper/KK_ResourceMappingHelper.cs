using Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using static ADV.Info;
using IllusionMods.Shared;

namespace IllusionMods
{
    public class KK_ResourceMappingHelper : ResourceMappingHelper
    {

        private const int MaxPersonalityId = 38;

        public KK_ResourceMappingHelper()
        {
            AddKnownLevels(24,50,51,52,53,54,55,56);

            if (IsStudio)
            {
                WhiteListPaths.Add("abdata/studio/info");
            }
            else
            {
                WhiteListPaths.Add("abdata/list/characustom");
                WhiteListPaths.Add("abdata/adv/scenario");
                WhiteListPaths.Add("abdata/communication");
                WhiteListPaths.Add("abdata/h/list");
                WhiteListPaths.Add("abdata/custom/customscenelist");
            }
            // ADV: most lines duplicated 3 times
            // adv/scenario/c[personality]/[*num]/[asset_num]
            RegisterMapping(AdvChecker, AdvMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.GameOnly);

            // communication: most lines duplicated between 7 times (some 35)
            // add extra mapping between 'communication_03' <=> 'communication_off_03': most lines 11, up to 55
            // communication/info_[*num]/[asset]
            RegisterMapping(CommunicationChecker, CommunicationMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.GameOnly);
       
            
            // Maker lists, very little duplication
            //  abdata/list/characustom/[*num]/[list_type]_[*num]

            // Subs
            //  abdata/h/list/[*num1]/personality_voice_c[personality]_[*num2]_[num3]
            // up to 38 dupes ignoring num1 + num2
            RegisterMapping(SubsChecker, SubsMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.GameOnly);
       

            // maker post lists
            //   - abdata/custom/customscenelist/cus_pose*
            RegisterMapping(MakerPoseChecker, MakerPoseMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.GameOnly);


            // studio item lists
            // abdata/studio/info/*/itemlist_*_01_*
            // Sync only due to startup slowdown
            RegisterMapping(StudioItemListChecker, StudioItemListMapper, ResourceMappingMode.Sync);

            // studio voice
            // abdata/studio/info/[*num1]/voice_[personality]_[*num2]_00
            // Sync only due to startup slowdown
            RegisterMapping(StudioVoiceChecker, StudioVoiceMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.StudioOnly);
 
            // studio voice category
            // abdata/studio/info/[*num1]/voicecategory_[*num1]_[personality]
            // same across ALL personalities though so heavy during studio load
            RegisterMapping(StudioVoiceCategoryChecker, StudioVoiceCategoryMapper, ResourceMappingMode.SyncAndReplacement, ResourceGameMode.StudioOnly);

            // personality names (in both studio and main game, sometimes even spelled the same)
            // abdata/list/characustom/[*num1]/cha_sample_voice_[*num1]
            // abdata/studio/info/[*num1]/voicegroup_[*num1]
            RegisterMapping(PersonalityNameChecker, PersonalityNameMapper, ResourceMappingMode.SyncAndReplacement); 
                    

        }

        private bool PersonalityNameChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            Logger.LogDebug($"{nameof(PersonalityNameChecker)}: {path}: {path.ResourcePathParts.Count}");

            if (path.ResourcePathParts.Count != 5) return false;

            if (path.ResourcePathParts[4].StartsWith("voicegroup_"))
            {
                return path.ResourcePathParts[1].Equals("studio", StringComparison.OrdinalIgnoreCase) &&
                       path.ResourcePathParts[2].Equals("info", StringComparison.OrdinalIgnoreCase);
            }

            if (path.ResourcePathParts[4].StartsWith("cha_sample_voice_"))
            {
                return path.ResourcePathParts[2].Equals("characustom", StringComparison.OrdinalIgnoreCase) &&
                       path.ResourcePathParts[1].Equals("list", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }


        private IEnumerable<ResourceMappingPath> PersonalityNameMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var gameMapPaths = new List<string> {"abdata", "list", "characustom", string.Empty, string.Empty};
            var studioMapPaths = new List<string> {"abdata", "studio", "info", string.Empty, string.Empty};

            var studioFirst = path.ResourcePathParts[1].Equals("studio", StringComparison.OrdinalIgnoreCase);

            // 0        1        2             3         4
            // abdata / list   / characustom / [*num1] / cha_sample_voice_[*num1]
            // abdata / studio / info        / [*num1] / voicegroup_[*num1]

            var secondary = new List<ResourceMappingPath>();
            foreach (var i in EnumerateLevels(99, 0))
            {
                var num1 = $"{i:00}";
                gameMapPaths[3] = studioMapPaths[3] = num1;
                gameMapPaths[4] = $"cha_sample_voice_{num1}";
                studioMapPaths[4] = $"voicegroup_{num1}";

                yield return ResourceMappingPath.FromParts(studioFirst ? studioMapPaths : gameMapPaths);
                secondary.Add(ResourceMappingPath.FromParts(studioFirst ? gameMapPaths : studioMapPaths));
            }

            foreach (var secondaryPath in secondary) yield return secondaryPath;
        }

        private bool StudioVoiceCategoryChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            if (mode == ResourceMappingMode.Replacement && !IsStudio) return false;
            if (path.ResourcePathParts.Count == 5 &&
                path.ResourcePathParts[4].StartsWith("voicecategory_") &&
                path.ResourcePathParts[1].Equals("studio", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("info", StringComparison.OrdinalIgnoreCase))
                
            {
                return true;
            }

            return false;
        }

        private IEnumerable<ResourceMappingPath> StudioVoiceCategoryMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            var itemParts = mappedParts[4].Split('_');
            foreach (var i in EnumerateLevels(99, 0))
            {
                mappedParts[3] = $"{i:00}";
                // for replacement only sync within personality
                if (mode == ResourceMappingMode.Replacement)
                {
                    itemParts[1] = mappedParts[3];
                    mappedParts[4] = string.Join("_", itemParts);
                    yield return ResourceMappingPath.FromParts(mappedParts);
                }

                if (mode == ResourceMappingMode.Sync)
                {
                    for (var j = MaxPersonalityId; j >= 0; j--)
                    {
                        // first number matches folder
                        itemParts[1] = mappedParts[3];
                        itemParts[2] = $"{j:00}";
                        mappedParts[4] = string.Join("_", itemParts);
                        yield return ResourceMappingPath.FromParts(mappedParts);
                    }
                }
            }

           
        }

        private bool StudioVoiceChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            if (path.ResourcePathParts.Count == 5 &&
                path.ResourcePathParts[4].StartsWith("voice_") &&
                path.ResourcePathParts[1].Equals("studio", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("info", StringComparison.OrdinalIgnoreCase))
                
            {
                return true;
            }

            return false;
        }

        private IEnumerable<ResourceMappingPath> StudioVoiceMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            var voiceParts = mappedParts[4].Split('_');
            foreach (var i in EnumerateLevels(99, 0))
            {
                mappedParts[3] = $"{i:00}";

                // max value for these is 9
                for (var j = 9; j >= 0; j--)
                {
                    // voice_[personality]_[j]_00
                    voiceParts[2] = $"{j:00}";
                    mappedParts[4] = string.Join("_", voiceParts);
                    yield return ResourceMappingPath.FromParts(mappedParts);
                }
            }
        }

        private bool StudioItemListChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            if (path.ResourcePathParts.Count == 5 &&
                path.ResourcePathParts[4].StartsWith("itemlist_") &&
                path.ResourcePathParts[1].Equals("studio", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("info", StringComparison.OrdinalIgnoreCase))
                
            {
                return true;
            }

            return false;
        }

        private IEnumerable<ResourceMappingPath> StudioItemListMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            var itemParts = mappedParts[4].Split('_');
            for (var i = 99; i >= 0; i--)
            {
                mappedParts[3] = $"{i:00}";
                for (var j = 99; j >= 0; j--)
                {
                    // first number matches folder
                    itemParts[1] = mappedParts[3];
                    itemParts[3] = $"{j:00}";
                    mappedParts[4] = string.Join("_", itemParts);
                    yield return ResourceMappingPath.FromParts(mappedParts);
                    // sometimes last number is single digit
                    if (j > 10) continue;
                    itemParts[3] = $"{j}";
                    mappedParts[4] = string.Join("_", itemParts);
                    yield return ResourceMappingPath.FromParts(mappedParts);
                }
            }
        }
        

        private bool MakerPoseChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            if (path.ResourcePathParts.Count == 4 &&
                path.ResourcePathParts[3].StartsWith("cus_pose") &&
                path.ResourcePathParts[1].Equals("custom", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("customscenelist", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private IEnumerable<ResourceMappingPath> MakerPoseMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            if (mappedParts[3].Equals("cus_pose", StringComparison.OrdinalIgnoreCase))
            {
                mappedParts[3] = "cus_pose_trial";
            }
            else if (mappedParts[3].Equals("cus_pose_trial", StringComparison.OrdinalIgnoreCase))
            {
                mappedParts[3] = "cus_pose";
            }
            else yield break;

            yield return ResourceMappingPath.FromParts(mappedParts);
            
        }

        private bool SubsChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            Logger.LogDebug($"ResourceMapping: {nameof(SubsChecker)} {path} -- {path.ResourcePathParts.Count}");
            if (path.ResourcePathParts.Count >= 5 &&
                path.ResourcePathParts[4].StartsWith("personality_voice_") &&
                path.ResourcePathParts[1].Equals("h", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                
                return true;
            }

            return false;
            
        }

        private IEnumerable<ResourceMappingPath> SubsMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            // abdata/h/list/[*num1]/personality_voice_c[personality]_[*num2]_[num3]
            var mappedParts = path.ResourcePathParts.ToList();
            var voiceParts = mappedParts[4].Split('_');
            foreach (var i in EnumerateLevels())
            {
                mappedParts[3] = $"{i:00}_00";
                // max num2 = 10
                for (var j = 10; j >= 0; j--)
                {
                    voiceParts[3] = $"{j:00}";
                    mappedParts[4] = string.Join("_", voiceParts);
                    yield return ResourceMappingPath.FromParts(mappedParts);

                }
            }
        }
       

        private bool CommunicationChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            if (path.ResourcePathParts.Count >= 3 &&
                path.ResourcePathParts[1].Equals("communication", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].StartsWith("info_"))
            {
                Logger.LogDebug($"ResourceMapping: {nameof(CommunicationChecker)} {path} -- {path.ResourcePathParts.Count}");
                return true;
            }

            return false;
            
        }

        private IEnumerable<ResourceMappingPath> CommunicationMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            var altMappedParts = mappedParts[3].StartsWith("communication_") ? mappedParts.ToList() : null;

            if (altMappedParts != null)
            {
                altMappedParts[3] = altMappedParts[3].StartsWith("communication_off_")
                    ? altMappedParts[3].Replace("communication_off_", "communication_")
                    : altMappedParts[3].Replace("communication_", "communication_off_");
            }

            foreach (var i in EnumerateLevels())
            {
                mappedParts[2] = $"info_{i:00}";
                yield return ResourceMappingPath.FromParts(mappedParts);
                if (altMappedParts != null)
                {
                    altMappedParts[2] = mappedParts[2];
                    yield return ResourceMappingPath.FromParts(altMappedParts);
                }
            }
        }

        private bool AdvChecker(ResourceMappingPath path, ResourceMappingMode mode)
        {
            /*
            return path.ResourcePathParts.Count >= 6 &&
                   path.ResourcePathParts[1].Equals("adv", StringComparison.OrdinalIgnoreCase) &&
                   path.ResourcePathParts[2].Equals("scenario", StringComparison.OrdinalIgnoreCase) &&
                   int.TryParse(path.ResourcePathParts[4], out var _);
            */
            if (path.ResourcePathParts.Count >= 5 &&
                path.ResourcePathParts[1].Equals("adv", StringComparison.OrdinalIgnoreCase) &&
                path.ResourcePathParts[2].Equals("scenario", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(path.ResourcePathParts[4], out var _))
            {
                Logger.LogDebug($"ResourceMapping: {nameof(AdvChecker)} {path} -- {path.ResourcePathParts.Count}");
                return true;
            }

            return false;

        }

        private IEnumerable<ResourceMappingPath> AdvMapper(ResourceMappingPath path, ResourceMappingMode mode)
        {
            var mappedParts = path.ResourcePathParts.ToList();
            foreach (var i in EnumerateLevels())
            {
                mappedParts[4] = $"{i:00}";
                yield return ResourceMappingPath.FromParts(mappedParts);
            }
        }
    }
}
