using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;

namespace Lastfm.Providers
{
    public class LastfmImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public LastfmImageProvider(IHttpClient httpClient, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _httpClient = httpClient;
            _config = config;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "last.fm"; }
        }

        public bool Supports(BaseItem item)
        {
            return item is MusicAlbum || item is MusicArtist;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            RemoteImageInfo info = null;

            var musicBrainzId = item is MusicAlbum ?
                item.GetProviderId(MetadataProviders.MusicBrainzAlbum) :
                item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(musicBrainzId))
            {
                var cachePath = Path.Combine(_config.ApplicationPaths.CachePath, "lastfm", musicBrainzId, "image.txt");

                try
                {
                    var parts = (await _fileSystem.ReadAllTextAsync(cachePath).ConfigureAwait(false)).Split('|');

                    info = GetInfo(parts.FirstOrDefault(), parts.LastOrDefault());
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (FileNotFoundException)
                {
                }
            }

            if (info == null)
            {
                var musicBrainzReleaseGroupId = item.GetProviderId(MetadataProviders.MusicBrainzReleaseGroup);

                if (!string.IsNullOrEmpty(musicBrainzReleaseGroupId))
                {
                    var cachePath = Path.Combine(_config.ApplicationPaths.CachePath, "lastfm", musicBrainzReleaseGroupId, "image.txt");

                    try
                    {
                        var parts = (await _fileSystem.ReadAllTextAsync(cachePath).ConfigureAwait(false)).Split('|');

                        info = GetInfo(parts.FirstOrDefault(), parts.LastOrDefault());
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }

            if (info != null)
            {
                list.Add(info);
            }

            // The only info we have is size
            return list.OrderByDescending(i => i.Width ?? 0);
        }

        private RemoteImageInfo GetInfo(string url, string size)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            var info = new RemoteImageInfo
            {
                ProviderName = Name,
                Url = url,
                Type = ImageType.Primary
            };

            if (string.Equals(size, "mega", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "extralarge", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "large", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (string.Equals(size, "medium", StringComparison.OrdinalIgnoreCase))
            {

            }

            return info;
        }

        public int Order
        {
            get
            {
                // After all others
                return 3;
            }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}
