﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;

namespace Lastfm.Providers
{
    public static class LastfmHelper
    {
        public static string GetImageUrl(IHasLastFmImages data, out string size)
        {
            size = null;

            if (data.image == null)
            {
                return null;
            }

            var validImages = data.image
                .Where(i => !string.IsNullOrWhiteSpace(i.url))
                .ToList();

            var img = validImages
                .FirstOrDefault(i => string.Equals(i.size, "mega", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "extralarge", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "large", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "medium", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault();

            if (img != null)
            {
                if (img.url.Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
                {
                    return null;
                }
                size = img.size;
                return img.url;
            }

            return null;
        }

        public static void SaveImageInfo(IApplicationPaths appPaths, ILogger logger, string musicBrainzId, string url, string size)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (string.IsNullOrEmpty(musicBrainzId))
            {
                throw new ArgumentNullException("musicBrainzId");
            }
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            var cachePath = Path.Combine(appPaths.CachePath, "lastfm", musicBrainzId, "image.txt");

            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    File.Delete(cachePath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                    File.WriteAllText(cachePath, url + "|" + size);
                }
            }
            catch (IOException ex)
            {
                // Don't fail if this is unable to write
                logger.ErrorException("Error saving to {0}", ex, cachePath);
            }
        }
    }
}
