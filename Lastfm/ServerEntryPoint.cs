﻿namespace Lastfm
{
    using Api;
    using Configuration;
    using MediaBrowser.Common.Net;   
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Plugins;
    using MediaBrowser.Controller.Session;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Logging;
    using MediaBrowser.Model.Serialization;
    using System.Linq;


    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager  _sessionManager;
        private readonly IUserDataManager _userDataManager;

        private LastfmApiClient _apiClient;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        public ServerEntryPoint(ISessionManager sessionManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogManager logManager, IUserDataManager userDataManager)
        {
            Plugin.Logger = logManager.GetLogger(Plugin.Instance.Name);

            _sessionManager  = sessionManager;
            _userDataManager = userDataManager;

            _apiClient = new LastfmApiClient(httpClient, jsonSerializer);
            
            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            //Bind events
            _sessionManager.PlaybackStart   += PlaybackStart;
            _sessionManager.PlaybackStopped += PlaybackStopped;
            _userDataManager.UserDataSaved  += UserDataSaved;
        }

        /// <summary>
        /// Let last fm know when a user favourites or unfavourites a track
        /// </summary>
        async void UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            //We also only care about User rating changes
            if (!e.SaveReason.Equals(UserDataSaveReason.UpdateUserRating))
                return;

            var lastfmUser = Utils.UserHelpers.GetUser(e.User.Id);
            if (lastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find user");
                return;
            }

            if (string.IsNullOrWhiteSpace(lastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;

            //Dont do if syncing
            if (Plugin.Syncing)
                return;

            await _apiClient.LoveTrack(item, lastfmUser, e.UserData.IsFavorite).ConfigureAwait(false);
        }


        /// <summary>
        /// Let last.fm know when a track has finished.
        /// Playback stopped is run when a track is finished.
        /// </summary>
        private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var item = e.Item as Audio;

            //Make sure the track has been fully played
            if (!e.PlayedToCompletion)
            {
                return;
            }

            //Played to completion will sometimes be true even if the track has only played 10% so check the playback ourselfs (it must use the app settings or something)
            //Make sure 80% of the track has been played back
            if (e.PlaybackPositionTicks == null)
            {
                Plugin.Logger.Debug("Playback ticks for {0} is null", item.Name);
                return;
            }

            var playPercent = ((double)e.PlaybackPositionTicks / item.RunTimeTicks) * 100;
            if (playPercent < 80)
            {
                Plugin.Logger.Debug("'{0}' only played {1}%, not scrobbling", item.Name, playPercent);
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

            var lastfmUser = Utils.UserHelpers.GetUser(user);
            if (lastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find last.fm user");
                return;
            }

            //User doesn't want to scrobble
            if (!lastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", user.Name, lastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(lastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Plugin.Logger.Info("No title present, aborting");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Artists.FirstOrDefault()))
            {
                Plugin.Logger.Info("No artist present, aborting");
                return;
            }

            await _apiClient.Scrobble(item, lastfmUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Let Last.fm know when a user has started listening to a track
        /// </summary>
        private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            //We only care about audio
            if (!(e.Item is Audio))
                return;

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

            var lastfmUser = Utils.UserHelpers.GetUser(user);
            if (lastfmUser == null)
            {
                Plugin.Logger.Debug("Could not find last.fm user");
                return;
            }

            //User doesn't want to scrobble
            if (!lastfmUser.Options.Scrobble)
            {
                Plugin.Logger.Debug("{0} ({1}) does not want to scrobble", user.Name, lastfmUser.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(lastfmUser.SessionKey))
            {
                Plugin.Logger.Info("No session key present, aborting");
                return;
            }

            var item = e.Item as Audio;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Plugin.Logger.Info("No title present, aborting");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Artists.FirstOrDefault()))
            {
                Plugin.Logger.Info("No artist present, aborting");
                return;
            }

            await _apiClient.NowPlaying(item, lastfmUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //Unbind events
            _sessionManager.PlaybackStart   -= PlaybackStart;
            _sessionManager.PlaybackStopped -= PlaybackStopped;
            _userDataManager.UserDataSaved  -= UserDataSaved;

            //Clean up
            _apiClient = null;

        }
    }
}
