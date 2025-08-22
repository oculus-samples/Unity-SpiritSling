// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Game configuration variables shared over networked
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    [Serializable]
    public struct TabletopGameSettings : INetworkStruct
    {
        [Tooltip("Seed for RNG")]
        public int seed;

        [Tooltip("True to show Meta avatars of other players")]
        public NetworkBool showAvatars;

        [Tooltip("Maximum duration (in seconds) of a phase for a player")]
        public byte timePhase;

        [Tooltip("Rounds (+ Nb Players) before CLOC is started ")]
        public byte roundsBeforeCloc;

        [Tooltip("false = CLOC is border, true = CLOC is N tiles/turn")]
        public NetworkBool isClocRandom;

        [Tooltip("Number of cells selected with random CLOC")]
        public byte clocRandomCount;

        [Tooltip("HP lost by units if they're in lava")]
        public byte lavaDamage;

        [Tooltip("Start Health Points given to every Kodama")]
        public byte kodamaStartHealthPoints;

        [Tooltip("How many tiles a Kodama can move around himself during the move phase")]
        public byte kodamaMoveRange;

        [Tooltip("How many tiles around a Kodama can be used to build a slingshot during the summon phase")]
        public byte kodamaSummonRange;

        [Tooltip("How many summons you can do every turn")]
        public byte kodamaSummonPerTurn;

        [Tooltip("Protect a kodama after in hit until it is its turn")]
        public NetworkBool kodamaHasImmunity;

        [Tooltip("Start Health Points given to every Slingshot")]
        public byte slingshotStartHealthPoints;

        [Tooltip("HP lost by a sling shot")]
        public byte slingshotDamage;

        [Tooltip("How many time can we shoot during a shoot phase per player per turn")]
        public byte slingBallShotsPerTurn;

        public byte lootboxStartHealthPoints;
        public byte lootboxDamage;
        public byte lootboxStartCount;
        public byte lootboxMinCount;
        public byte lootBoxSpawnCountPerTurn;
        public byte lootBoxHeightDownAmount;
        public byte lootBoxHeightDownStrongAmount;
        public byte lootBoxHeightUpAmount;
        public byte lootBoxHeightUpStrongAmount;
        public byte lootBoxNormalStrongChances;
    }

    /// <summary>
    /// Networked holder for current game data
    /// </summary>
    public class TabletopGameSettingsHolder : NetworkBehaviour
    {
        [Networked]
        public TabletopGameSettings GameSettings { get; set; }
    }
}
