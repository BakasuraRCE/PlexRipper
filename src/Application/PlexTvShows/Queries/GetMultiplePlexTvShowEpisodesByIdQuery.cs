﻿using System.Collections.Generic;
using FluentResults;
using MediatR;
using PlexRipper.Domain;

namespace PlexRipper.Application.PlexTvShows
{
    public class GetMultiplePlexTvShowEpisodesByIdQuery : IRequest<Result<List<PlexTvShowEpisode>>>
    {
        public GetMultiplePlexTvShowEpisodesByIdQuery(List<int> ids, bool includeTvShowAndSeason = false, bool includeLibrary = false, bool includeServer = false)
        {
            Ids = ids;
            IncludeTvShowAndSeason = includeTvShowAndSeason;
            IncludeLibrary = includeLibrary;
            IncludeServer = includeServer;
        }

        public List<int> Ids { get; }

        public bool IncludeTvShowAndSeason { get; }

        public bool IncludeLibrary { get; }

        public bool IncludeServer { get; }
    }
}