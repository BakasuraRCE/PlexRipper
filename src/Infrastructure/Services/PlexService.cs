﻿using Ardalis.GuardClauses;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlexRipper.Application.Common.Interfaces;
using PlexRipper.Domain.Entities;
using PlexRipper.Domain.Entities.Plex;
using PlexRipper.Infrastructure.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlexRipper.Infrastructure.Services
{
    public class PlexService : IPlexService
    {

        #region Private Fields

        private const string FriendsUri = "https://plex.tv/pms/friends/all";
        private const string GetAccountUri = "https://plex.tv/users/account.json";
        private const string PlexServersUrl = "https://plex.tv/pms/servers.xml";
        private const string PlexSignInUrl = "https://plex.tv/users/sign_in.json";
        private static readonly HttpClient client = new HttpClient();
        private readonly IPlexApi _plexApi;
        private readonly IPlexRipperDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PlexService> _logger;

        #endregion Private Fields

        #region Public Constructors

        public PlexService(IPlexRipperDbContext context, IMapper mapper, IPlexApi plexApi, ILogger<PlexService> logger)
        {
            _plexApi = plexApi;
            _context = context;
            _mapper = mapper;
            _logger = logger;
            client.Timeout = new TimeSpan(0, 0, 0, 30);

        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<PlexAccount> AddOrUpdatePlexAccount(Account account, PlexAccount plexAccountDto)
        {
            if (plexAccountDto == null)
            {
                _logger.LogError($"PlexAccountDTO given as a parameter in {nameof(AddOrUpdatePlexAccount)} was null.");
                return null;
            }


            var accountDB = await _context.Accounts.FindAsync(account.Id);

            PlexAccount plexAccount = _mapper.Map<PlexAccount>(plexAccountDto);
            var result = await _context.PlexAccounts.FindAsync(plexAccount.Id);
            if (result != null)
            {
                _logger.LogDebug($"PlexAccount with Id: {result.Id} already exists, will update now");
                // Update
                result = plexAccount;
                plexAccount.Account = accountDB;
                result.ConfirmedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return result;

            }

            // Add
            _logger.LogDebug($"PlexAccount with Id: {plexAccount.Id} does not yet exist, will add now");
            plexAccount.ConfirmedAt = DateTime.Now;
            plexAccount.Account = accountDB;
            await _context.PlexAccounts.AddAsync(plexAccount);
            await _context.SaveChangesAsync();
            return plexAccount;
        }

        public PlexAccount GetPlexAccount(long plexAccountId)
        {
            return _context.PlexAccounts
                .Include(x => x.PlexAccountServers)
                .FirstOrDefault(x => x.Id == plexAccountId);
        }

        /// <summary>
        /// Returns the <see cref="PlexAccount"/> associated with this <see cref="Account"/>
        /// </summary>
        /// <param name="account">The <see cref="Account"/> to use</param>
        /// <returns>Can return null when invalid</returns>
        public PlexAccount ConvertToPlexAccount(Account account)
        {
            if (account == null)
            {
                _logger.LogWarning("The account was null");
                return null;
            }

            if (!account.IsValidated)
            {
                _logger.LogWarning(
                    $"The account with Id: {account.Id} has not yet been confirmed." +
                           $" Confirm first before using ConvertToPlexAccount()");
                return null;
            }

            account = _context
                   .Accounts
                   .Include(x => x.PlexAccount)
                   .ThenInclude(x => x.PlexAccountServers)
                   .FirstOrDefault(x => x.Id == account.Id);
            return account?.PlexAccount;
        }

        public async Task<string> GetPlexToken(PlexAccount plexAccount)
        {
            Guard.Against.Null(plexAccount, nameof(plexAccount));

            if (plexAccount.AuthToken != string.Empty)
            {
                // TODO Make the token refresh limit configurable 
                if ((plexAccount.ConfirmedAt - DateTime.Now).TotalDays < 30)
                {
                    _logger.LogInformation("Plex AuthToken was still valid, using from local DB.");
                    return plexAccount.AuthToken;
                }
                _logger.LogInformation("Plex AuthToken has expired, refreshing Plex AuthToken now.");

                return await _plexApi.RefreshPlexAuthTokenAsync(plexAccount.Account);
            }

            _logger.LogError($"PlexAccount with Id: {plexAccount.Id} contained an empty AuthToken!");
            return string.Empty;
        }



        public async Task<List<PlexServer>> GetServers(PlexAccount plexAccount, bool refresh = false)
        {

            Guard.Against.Null(plexAccount, nameof(plexAccount));

            if (refresh || plexAccount.PlexAccountServers.Count == 0)
            {
                var token = await GetPlexToken(plexAccount);

                if (!string.IsNullOrEmpty(token))
                {
                    var result = await _plexApi.GetServer(token);
                    var conversion = _mapper.Map<List<PlexServer>>(result.Server);
                    await AddOrUpdatePlexServers(plexAccount, conversion);
                }
            }

            try
            {
                // Retrieve all servers
                var serverList = await _context.PlexServers
                    .Include(x => x.PlexAccountServers)
                    .ThenInclude(x => x.PlexAccount)
                    .Where(x => x.PlexAccountServers
                        .Any(y => y.PlexAccountId == plexAccount.Id))
                    .ToListAsync();

                return serverList;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception: ", e);
                throw;
            }
        }




        private async Task AddOrUpdatePlexServers(PlexAccount plexAccount, List<PlexServer> servers)
        {
            foreach (var plexServer in servers)
            {
                var plexServerDB =
                    _context.PlexServers.FirstOrDefault(x => x.MachineIdentifier == plexServer.MachineIdentifier);
                if (plexServerDB != null)
                {
                    plexServerDB = plexServer;
                    _context.PlexServers.Update(plexServerDB);
                }
                else
                {
                    // Add
                    await _context.PlexServers.AddAsync(plexServer);
                    // Create entry in many-to-many table
                    var plexAccountServer = new PlexAccountServer
                    {
                        PlexAccount = plexAccount,
                        PlexServer = plexServer
                    };
                    plexServer.PlexAccountServers = new List<PlexAccountServer>();
                    plexServer.PlexAccountServers.Add(plexAccountServer);
                    await _context.PlexServers.AddAsync(plexServer);
                }
            }
            await _context.SaveChangesAsync();
        }


        public async Task<PlexLibrary> GetLibrary(PlexServer plexServer)
        {
            var plexLibraries = await GetLibrariesAsync(plexServer);
            return plexLibraries.First();
            //var result = await _plexApi.GetLibrary(plexServer.AccessToken, plexServer.BaseUrl, library.Key);
            //var metaData = await _plexApi.GetMetadata(plexServer.AccessToken, plexServer.BaseUrl, 5516);
            //string downloadUrl = _plexApi.GetDownloadUrl(plexServer, metaData);
            //string filename = _plexApi.GetDownloadFilename(plexServer, metaData);
            //_plexApi.DownloadMedia(plexServer.AccessToken, downloadUrl, filename);
            //return plexContainer;
        }

        /// <summary>
        /// Returns the list of all libraries belonging to this <see cref="PlexServer"/>
        /// </summary>
        /// <param name="plexServer">The PlexServer to retrieve the libraries</param>
        /// <param name="refresh">Force an Plex API update with the latest data</param>
        /// <returns>List of <see cref="PlexLibrary"/></returns>
        public async Task<List<PlexLibrary>> GetLibrariesAsync(PlexServer plexServer, bool refresh = false)
        {
            Guard.Against.Null(plexServer, nameof(plexServer));

            var plexServerDB = await _context.PlexServers.FindAsync(plexServer.Id);

            if (plexServerDB != null && (plexServerDB.PlexLibraries == null || plexServerDB.PlexLibraries.Count == 0) || refresh)
            {
                var plexContainer = await _plexApi.GetLibrarySections(plexServer.AccessToken, plexServer.BaseUrl);
                var librariesDTOs = plexContainer.MediaContainer.Directory.ToList();

                var libraries = await _context.PlexLibraries.Where(x => x.PlexServer.Id == plexServer.Id).ToListAsync();

                var librariesNotToRemove = new List<int>();

                // Update or add new plex libraries
                foreach (var libraryDTO in librariesDTOs)
                {
                    var libraryDB = libraries.Find(x => x.Key == libraryDTO.Key);

                    if (libraryDB != null)
                    {
                        // Update
                        libraryDB.Count = libraryDTO.Count;
                        libraryDB.Key = libraryDTO.Key;
                        libraryDB.Title = libraryDTO.Title;
                        libraryDB.HasAccess = true;
                        libraryDB.PlexServer = plexServerDB;
                        librariesNotToRemove.Add(libraryDB.Id);
                    }
                    else
                    {
                        // Add
                        libraryDB = _mapper.Map<PlexLibrary>(libraryDTO);
                        libraryDB.HasAccess = true;
                        libraryDB.PlexServer = plexServerDB;
                        await _context.PlexLibraries.AddAsync(libraryDB);
                    }
                }

                // Soft-delete any library that were either not updated or added
                foreach (var plexLibrary in libraries)
                {
                    if (!librariesNotToRemove.Contains(plexLibrary.Id))
                    {
                        var x = await _context.PlexLibraries.FindAsync(plexLibrary.Id);
                        x.HasAccess = false;
                    }
                }

                await _context.SaveChangesAsync();

            }

            return await _context.PlexLibraries.Where(x => x.PlexServer.Id == plexServer.Id).ToListAsync();
        }

        public async Task<List<PlexLibrary>> GetLibrariesByPlexServerIdAsync(int plexServerId, bool refresh = false)
        {
            Guard.Against.Zero(plexServerId, nameof(plexServerId));

            var plexServer = await _context.PlexServers.FindAsync(plexServerId);

            Guard.Against.Null(plexServer, nameof(plexServer));

            return await GetLibrariesAsync(plexServer);

        }

        /// <summary>
        /// Check the validity of Plex credentials to the Plex API. 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The result of the test</returns>
        public async Task<bool> IsPlexAccountValid(string username, string password)
        {
            return await RequestPlexAccountAsync(username, password) != null;
        }


        public async Task<PlexAccount> RequestPlexAccountAsync(string username, string password)
        {
            var plexAuthentication = await _plexApi.PlexSignInAsync(username, password);
            return _mapper.Map<PlexAccount>(plexAuthentication?.User);

        }

        #endregion Public Methods

    }
}
