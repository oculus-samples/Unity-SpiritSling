// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Static events available related to table top gameplay
    /// </summary>
    public static class TabletopGameEvents
    {
        public delegate void OnCellInLavaHandler(HexCell cell);
        public delegate void OnNextTurnCalledHandler(byte nextPlayerIndex, bool forceNoNewRound);
        public delegate void OnGamePhaseChangedHandler(BaseTabletopPlayer player, TableTopPhase phase);
        public delegate void OnPlayerEventHandler(BaseTabletopPlayer player);
        public delegate void OnPlayerIndexEventHandler(int playerIndex);
        public delegate void OnLootBoxPickedUpHandler(LootBox box, Kodama kodama);
        public delegate void OnLootBoxDestroyedHandler(LootBox comp);
        public delegate void PawnEventHandler(Pawn comp);
        public delegate void GenericEventHandler();

        public static GenericEventHandler OnFirstMenuEnter;
        public static GenericEventHandler OnMainMenuEnter;
        public static GenericEventHandler OnLobbyEnter;
        public static OnPlayerEventHandler OnPlayerJoin;
        public static OnPlayerEventHandler OnPlayerLeave;
        public static OnGamePhaseChangedHandler OnGamePhaseChanged;
        public static OnNextTurnCalledHandler OnNextTurnCalled;
        public static OnPlayerEventHandler OnGameOver;
        public static OnPlayerEventHandler OnWin;
        public static OnPlayerIndexEventHandler OnBoardClearedAfterVictory;
        public static GenericEventHandler OnGameBoardReady;
        public static GenericEventHandler OnSetupComplete;
        public static OnLootBoxPickedUpHandler OnLootBoxPickedUp;
        public static OnLootBoxDestroyedHandler OnLootBoxDestroyed;
        public static GenericEventHandler OnRequestQuitGame;
        public static GenericEventHandler OnRequestLeaveToLobby;
        public static GenericEventHandler OnRequestSkipPhase;
        public static GenericEventHandler OnPawnDragStart;
        public static PawnEventHandler OnPawnDragEnd;
        public static GenericEventHandler OnPawnDragCanceled;
        public static GenericEventHandler OnShootPhaseEnd;
        public static GenericEventHandler GameStart;
        public static GenericEventHandler GameClocStart;
        public static GenericEventHandler OnConnectionManagerShutdown;
        public static OnPlayerEventHandler OnKodamaDeath;
    }
}