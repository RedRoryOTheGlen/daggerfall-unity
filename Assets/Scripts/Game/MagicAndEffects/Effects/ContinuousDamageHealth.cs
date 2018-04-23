﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// Continuous Damage - Health
    /// </summary>
    public class ContinuousDamageHealth : BaseEntityEffect
    {
        public override string Key { get { return "ContinuousDamage-Health"; } }
        public override string GroupName { get { return TextManager.Instance.GetText("ClassicEffects", "continuousDamage"); } }
        public override string SubGroupName { get { return TextManager.Instance.GetText("ClassicEffects", "health"); } }
        public override int ClassicGroup { get { return 1; } }
        public override int ClassicSubGroup { get { return 0; } }
        public override TextFile.Token[] SpellMakerDescription { get { return DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1504); } }
        public override TextFile.Token[] SpellBookDescription { get { return DaggerfallUnity.Instance.TextProvider.GetRSCTokens(1204); } }
        public override bool SupportChance { get { return false; } }
        public override TargetTypes AllowedTargets { get { return EntityEffectBroker.TargetFlags_Other; } }
        public override ElementTypes AllowedElements { get { return EntityEffectBroker.ElementFlags_All; } }

        public override void MagicRound(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            // Get peered entity gameobject
            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            // Implement effect
            int magnitude = GetMagnitude(caster);
            entityBehaviour.DamageHealthFromSource(caster, magnitude, false, Vector3.zero);

            //Debug.LogFormat("Effect {0} damaged {1} by {2} health points and has {3} magic rounds remaining.", Key, entityBehaviour.name, magnitude, RoundsRemaining - 1);
        }
    }
}