using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using APIClient;
using Conductor.Client.Models;
using Newtonsoft.Json;
using Workers.Dtos;
using Task = System.Threading.Tasks.Task;

namespace Workers.Application
{
    public class FolderServices
    {
        private readonly FlorenceApi _clientApi;

        public FolderServices(FlorenceApi clientApi)
        {
            _clientApi = clientApi;
        }

        public async Task<Folder[]> BuildFolderTreeAsync(string teamId)
        {
            var allFolders = await GetAllFoldersAsync(teamId);
            var rootFolders = new List<Folder>();

            var childToParentMap = new Dictionary<string, string>();
            foreach (var folder in allFolders)
            {
                var subFolderResponse = await _clientApi.GetAsync($"/teams/{teamId}/folders/{folder.id}/sub-folders", "");
                var subFolderIds = JsonConvert.DeserializeObject<List<Folder>>(subFolderResponse);
                foreach (var subFolderId in subFolderIds)
                {
                    childToParentMap[subFolderId.id] = folder.id;
                }
            }

            var topLevelFolderIds = allFolders.Select(f => f.id).Except(childToParentMap.Keys).ToList();

            foreach (var id in topLevelFolderIds)
            {
                var topLevelFolder = allFolders.FirstOrDefault(f => f.id == id);
                if (topLevelFolder != null)
                {
                    rootFolders.Add(topLevelFolder);
                    await BuildSubFoldersAsync(teamId, topLevelFolder, allFolders, childToParentMap);
                }
            }

            return rootFolders.ToArray();
        }

        private async Task<List<Folder>> GetAllFoldersAsync(string teamId)
        {
            var response = await _clientApi.GetAsync($"/teams/{teamId}/folders", "");
            var folders = JsonConvert.DeserializeObject<List<Folder>>(response);

            return folders;
        }

        private async Task BuildSubFoldersAsync(string teamId, Folder parentFolder, List<Folder> allFolders, Dictionary<string, string> childToParentMap)
        {
            var childFolderIds = childToParentMap.Where(kvp => kvp.Value == parentFolder.id).Select(kvp => kvp.Key).ToList();

            foreach (var id in childFolderIds)
            {
                var subFolder = allFolders.FirstOrDefault(f => f.id == id);
                if (subFolder != null)
                {
                    parentFolder.SubFolders.Add(subFolder);
                    await BuildSubFoldersAsync(teamId, subFolder, allFolders, childToParentMap);
                }
            }

            await PopulateFolderContentsAsync(teamId, parentFolder);
        }

        private async Task PopulateFolderContentsAsync(string teamId, Folder folder)
        {
            var contentsResponse = await _clientApi.GetAsync($"/teams/{teamId}/folders/{folder.id}/contents", "");
            var contents = JsonConvert.DeserializeObject<List<Content>>(contentsResponse);

            folder.Contents = contents;
        }

        
    }
}
