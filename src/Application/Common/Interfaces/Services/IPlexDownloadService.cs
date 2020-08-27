﻿using FluentResults;
using PlexRipper.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlexRipper.Domain.Enums;

namespace PlexRipper.Application.Common.Interfaces
{
    public interface IPlexDownloadService
    {
        Task<string> GetPlexTokenAsync(PlexAccount plexAccount);
        Task<Result<bool>> DownloadMovieAsync(int plexAccountId, int plexMovieId);
        Task<Result<DownloadTask>> GetDownloadTaskAsync(int plexAccountId, PlexMovie plexMovie);
        Task<Result<List<PlexServer>>> GetAllDownloadsAsync();
        Task<Result<bool>> DeleteDownloadsAsync(int downloadTaskId);
        Task<Result<bool>> DownloadTvShowAsync(int plexAccountId, int mediaId, PlexMediaType type);
        Result<bool> StopDownloadTask(int downloadTaskId);
        Task<Result<bool>> RestartDownloadTask(int downloadTaskId);
    }
}
