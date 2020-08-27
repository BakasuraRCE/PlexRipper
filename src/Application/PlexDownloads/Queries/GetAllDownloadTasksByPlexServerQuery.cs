﻿using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PlexRipper.Application.Common.Interfaces.DataAccess;
using PlexRipper.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlexRipper.Application.Common.Base;

namespace PlexRipper.Application.PlexDownloads.Queries
{
    public class GetAllDownloadTasksByPlexServerQuery : IRequest<Result<List<PlexServer>>>
    {
        public GetAllDownloadTasksByPlexServerQuery(bool includePlexAccount = false, bool includeFolderPaths = false,
            bool includeServerStatus = false)
        {
            IncludeServerStatus = includeServerStatus;
            IncludePlexAccount = includePlexAccount;
            IncludeFolderPaths = includeFolderPaths;
        }

        public bool IncludeServerStatus { get; }
        public bool IncludePlexAccount { get; }
        public bool IncludeFolderPaths { get; }
    }

    public class GetAllDownloadTasksByPlexServerQueryValidator : AbstractValidator<GetAllDownloadTasksByPlexServerQuery>
    {
        public GetAllDownloadTasksByPlexServerQueryValidator() { }
    }


    public class GetAllDownloadTasksByPlexServerQueryHandler : BaseHandler,
        IRequestHandler<GetAllDownloadTasksByPlexServerQuery, Result<List<PlexServer>>>
    {
        public GetAllDownloadTasksByPlexServerQueryHandler(IPlexRipperDbContext dbContext) : base(dbContext) { }

        public async Task<Result<List<PlexServer>>> Handle(GetAllDownloadTasksByPlexServerQuery request, CancellationToken cancellationToken)
        {
            IQueryable<PlexServer> query = _dbContext.PlexServers
                .Include(x => x.PlexLibraries)
                .ThenInclude(x => x.DownloadTasks)
                .ThenInclude(x => x.PlexServer);
            if (request.IncludePlexAccount)
            {
                query = query
                    .Include(x => x.PlexLibraries)
                    .ThenInclude(x => x.DownloadTasks)
                    .ThenInclude(x => x.PlexAccount);
            }
            if (request.IncludeFolderPaths)
            {
                query = query
                    .Include(x => x.PlexLibraries)
                    .ThenInclude(x => x.DownloadTasks)
                    .ThenInclude(x => x.DownloadFolder)
                    .Include(x => x.PlexLibraries)
                    .ThenInclude(x => x.DownloadTasks)
                    .ThenInclude(x => x.DestinationFolder);
            }
            if (request.IncludeServerStatus)
            {
                query = query.Include(x => x.ServerStatus);
            }
            var serverList = await query
                .Where(x => x.PlexLibraries.Any(y => y.DownloadTasks.Any()))
                .ToListAsync(cancellationToken);
            return Result.Ok(serverList);
        }
    }
}