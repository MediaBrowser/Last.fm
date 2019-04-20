namespace Lastfm.Models.Responses
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class GetArtistTracksResponse : BaseResponse
    {
        [DataMember(Name= "artisttracks")]
        public GetArtistTracks ArtistTracks { get; set; }

        public bool HasTracks()
        {
            return ArtistTracks != null && ArtistTracks.Tracks != null && ArtistTracks.Tracks.Count > 0;
        }
    }

    [DataContract]
    public class GetArtistTracks
    {
        [DataMember(Name="track")]
        public List<LastfmArtistTrack> Tracks { get; set; }

        [DataMember(Name = "@attr")]
        public GetArtistTracksMeta Metadata { get; set; }
    }

    [DataContract]
    public class GetArtistTracksMeta
    {
        [DataMember(Name = "totalPages")]
        public int TotalPages { get; set; }

        [DataMember(Name = "total")]
        public int TotalTracks { get; set; }

        [DataMember(Name = "page")]
        public int Page { get; set; }

        public bool IsLastPage()
        {
            return Page >= TotalPages;
        }
    }
}
