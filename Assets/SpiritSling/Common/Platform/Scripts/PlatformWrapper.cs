// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace SpiritSling
{
    public class PlatformWrapper : MonoBehaviour
    {
        public delegate void OnLaunchParamsChangedHandler();

        public static OnLaunchParamsChangedHandler OnLaunchParamsChanged;
        public static PlatformWrapper Instance;

        private List<string> m_destinationAPINames = new List<string>();
        private string m_trackingID;

        private void OnDestroy()
        {
            AppEntitlementCheck.OnPlatformReady -= OnPlatformReady;
        }

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            AppEntitlementCheck.OnPlatformReady += OnPlatformReady;
        }

        private void OnPlatformReady()
        {
            ListenForNotifications();
        }

        /// <summary>
        /// Updates the platform by running callbacks for all messages being returned.
        /// </summary>
        public void Update()
        {
            Request.RunCallbacks();
        }

        #region Destinations

        /// <summary>
        /// Retrieves the list of available destinations.
        /// </summary>
        public void GetDestinations()
        {
            RichPresence.GetDestinations().OnComplete(OnGetDestinations);
        }

        /// <summary>
        /// Callback for handling the retrieved list of destinations.
        /// </summary>
        /// <param name="message">Message containing the destination list.</param>
        private void OnGetDestinations(Message<DestinationList> message)
        {
            if (message.IsError)
            {
                Log.Debug("[Platform] Destination: Could not get the list of destinations!");
            }
            else
            {
                foreach (var destination in message.Data)
                {
                    m_destinationAPINames.Add(destination.ApiName);
                    Log.Debug("[Platform] Destination:" + destination.ApiName);
                }
            }
        }

        #endregion

        #region Launch Params

        /// <summary>
        /// Sets up a listener for notifications about launch intent changes.
        /// </summary>
        public void ListenForNotifications()
        {
            ApplicationLifecycle.SetLaunchIntentChangedNotificationCallback(OnLaunchIntentChangeNotif);
        }

        /// <summary>
        /// Gets the application launch details including deep link information.
        /// </summary>
        /// <returns>String containing the launch details.</returns>
        public LaunchDetails GetAppLaunchDetails()
        {
            var launchDetails = ApplicationLifecycle.GetLaunchDetails();

            m_trackingID = !string.IsNullOrEmpty(launchDetails.TrackingID) ? launchDetails.TrackingID : "FakeTrackingID";

            Log.Debug("[Platform] GetAppLaunchDetails DestinationApiName " + launchDetails.DestinationApiName);
            Log.Debug("[Platform] GetAppLaunchDetails MatchSessionID " + launchDetails.MatchSessionID);

            return launchDetails;
        }

        /// <summary>
        /// Callback for handling launch intent change notifications.
        /// </summary>
        /// <param name="message">Message containing the new launch intent details.</param>
        private void OnLaunchIntentChangeNotif(Message<string> message)
        {
            if (message.IsError)
            {
                Log.Error("[Platform] Launch:" + message.GetError().Message);
            }
            else
            {
                Log.Debug("[Platform] Updateded launch details");
                OnLaunchParamsChanged?.Invoke();
            }
        }

        #endregion

        #region Users

        /// <summary>
        /// Retrieves the currently logged-in user.
        /// </summary>
        public void GetLoggedInUser()
        {
            Users.GetLoggedInUser().OnComplete(OnGetUserCallback);
        }

        /// <summary>
        /// Retrieves a user by their user ID.
        /// </summary>
        /// <param name="userID">The ID of the user to retrieve.</param>
        public void GetUser(string userID)
        {
            Users.Get(Convert.ToUInt64(userID)).OnComplete(OnGetUserCallback);
        }

        /// <summary>
        /// Retrieves the friends of the currently logged-in user.
        /// </summary>
        public void GetLoggedInFriends()
        {
            Users.GetLoggedInUserFriends().OnComplete(OnGetFriendsCallback);
        }

        /// <summary>
        /// Logs details of a list of users.
        /// </summary>
        /// <param name="users">The list of users to log.</param>
        public void DumpUsers(UserList users)
        {
            foreach (var user in users)
            {
                DumpUser(user);
            }
        }

        /// <summary>
        /// Logs details of a single user.
        /// </summary>
        /// <param name="user">The user to log.</param>
        public void DumpUser(User user)
        {
            Log.Debug("[Platform] User: " + user.ID + " " + user.OculusID + " " + user.Presence + " " + user.DisplayName);
            Log.Debug(
                "[Platform] Presence: " + user.PresenceStatus + " " + user.PresenceDeeplinkMessage + " " + user.PresenceDestinationApiName + " "
                + user.PresenceLobbySessionId + " " + user.PresenceMatchSessionId);
        }

        /// <summary>
        /// Callback for handling the retrieved user information.
        /// </summary>
        /// <param name="msg">Message containing the user information.</param>
        void OnGetUserCallback(Message<User> msg)
        {
            if (!msg.IsError)
            {
                Log.Debug("[Platform] User: Received get user success");
                DumpUser(msg.Data);
            }
            else
            {
                Log.Error("[Platform] User: Received get user error");
                var error = msg.GetError();
                Log.Error("[Platform] User Error: " + error.Message);
            }
        }

        /// <summary>
        /// Callback for handling the retrieved friends information.
        /// </summary>
        /// <param name="msg">Message containing the friends list.</param>
        void OnGetFriendsCallback(Message<UserList> msg)
        {
            if (!msg.IsError)
            {
                Log.Debug("[Platform] Friends: Received get friends success");
                var users = msg.Data;
                DumpUsers(users);
            }
            else
            {
                Log.Error("[Platform] Friends: Received get friends error");
                var error = msg.GetError();
                Log.Error("[Platform] Friends Error: " + error.Message);
            }
        }

        #endregion

        #region Presence

        /// <summary>
        /// Sets the presence of the user in the specified destination, lobby, and room.
        /// </summary>
        /// <param name="destination">The API name of the destination.</param>
        /// <param name="lobby">The lobby session ID.</param>
        /// <param name="room">The match session ID.</param>
        public void SetPresence(string destination, string lobby, string room)
        {
            Log.Debug("[Platform] Set Presence in:" + destination + " room:" + room);

            var options = new GroupPresenceOptions();

            options.SetDestinationApiName(destination);
            options.SetMatchSessionId(room);
            options.SetLobbySessionId(lobby);
            options.SetIsJoinable(true);

            _ = GroupPresence.Set(options).OnComplete(
                message =>
                {
                    if (message.IsError)
                    {
                        Log.Error("[Platform] Error in setting presence: " + message.GetError().Message);
                    }
                    else
                    {
                        Log.Debug("[Platform] Group presence successfully set!");
                    }
                });
        }

        /// <summary>
        /// Get informations on the invites sent
        /// </summary>
        public void GetSentInvites()
        {
            Log.Debug("[Platform] GetSentInvites");
            var sentInvites = GroupPresence.GetSentInvites().OnComplete(
                message =>
                {
                    if (message.IsError)
                    {
                        Log.Error("[Platform] Error in GetSentInvites: " + message.GetError().Message);
                    }
                    else
                    {
                        Log.Debug("[Platform] GetSentInvites successfully done!");
                        Log.Debug("[Platform] Invites count:" + message.Data.Count);
                    }
                });
        }

        /// <summary>
        /// Mark the invitation as closed
        /// </summary>
        public void CloseInvitation()
        {
            _ = GroupPresence.SetIsJoinable(false);
            Log.Debug("[Platform] SetIsJoinable false");
        }

        /// <summary>
        /// Clear the user presence from the destination
        /// </summary>
        public void ClearPresence()
        {
            _ = GroupPresence.Clear();
            Log.Debug("[Platform] Clear Presence");
        }

        /// <summary>
        /// Launches the invite panel for users to invite others.
        /// </summary>
        public void LaunchInvitePanel(Message<InvitePanelResultInfo>.Callback callback,
            Message<GroupPresenceJoinIntent>.Callback joinIntentCallback,
            Message<GroupPresenceLeaveIntent>.Callback leaveIntentCallback)
        {
            Log.Debug("[Platform] Launching Invite Panel...");
            var options = new InviteOptions();

            var request = GroupPresence.LaunchInvitePanel(options).OnComplete(callback);
            GroupPresence.SetJoinIntentReceivedNotificationCallback(joinIntentCallback);
            GroupPresence.SetLeaveIntentReceivedNotificationCallback(leaveIntentCallback);
        }

        #endregion
    }
}