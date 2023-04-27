using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using APIClient;
using Conductor.Client.Models;
using Conductor.Definition.TaskType;
using Newtonsoft.Json;
using Workers.Dtos;
using File = System.IO.File;
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

        public async Task<List<Folder>> BuildFolderTreeAsync(string teamId)
        {
            var allFolders = await GetAllFoldersAsync(teamId);

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
            var topLevelFolders = topLevelFolderIds.Select(id => allFolders.FirstOrDefault(f => f.id == id)).Where(folder => folder != null).ToList();

            foreach (var topLevelFolder in topLevelFolders)
            {
                await BuildSubFoldersAsync(teamId, topLevelFolder, allFolders, childToParentMap);
            }

            await GetContentDetails(teamId, topLevelFolders).ConfigureAwait(false);

            return topLevelFolders;
        }

        private async Task GetContentDetails(string teamId, List<Folder> topLevelFolders)
        {
            foreach (var folder in topLevelFolders)
            {
                var tasks = new List<Task>();
                foreach (var c in folder?.Contents)
                {
                    tasks.Add(PopulateContentsAsync(teamId, c));
                    if (tasks.Count > 5)
                    {
                        Task.WaitAll(tasks.ToArray());
                        tasks.Clear();
                    }
                }
                if (folder.SubFolders.Any())
                {
                    await GetContentDetails(teamId,folder.SubFolders).ConfigureAwait(false);
                }
                Task.WaitAll(tasks.ToArray());
            }
        }


        private async Task<List<Folder>> GetAllFoldersAsync(string teamId)
        {
            int i = 1;
            var folders = new List<Folder>();
            while (true)
            {
                var response = await _clientApi.GetAsync($"/teams/{teamId}/folders", $"page={i}&perPage=50");
                var folder = JsonConvert.DeserializeObject<List<Folder>>(response);
                if (folder != null && folder.Any())
                {
                    i++;
                    folders.AddRange(folder);
                    continue;
                }
                break;
            }
            
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

            var i = 1;
            var contents = new List<Content>();
            while (true)
            {
                var contentsResponse = await _clientApi.GetAsync($"/teams/{teamId}/folders/{folder.id}/contents", $"page={i}&perPage=50");
                var contentList = JsonConvert.DeserializeObject<List<Content>>(contentsResponse);
                if (contentList != null && contentList.Any())
                {
                    i++;
                    contents.AddRange(contentList);
                    continue;
                }
                break;
            }

            folder.Contents = contents;

        }


        private async Task PopulateContentsAsync(string teamId, Content content)
        {

            var i = 1;
            var contents = new List<Content>();
            while (true)
            {
                var contentsResponse = await _clientApi.GetAsync($"/teams/{teamId}/contents/{content.id}", $"v");
                var fileContent = JsonConvert.DeserializeObject<FileContent>(contentsResponse);
                if (fileContent != null)
                {
                    content.md5 = fileContent.file.md5;
                    content.size = fileContent.file.size;
                }
                break;
            }

        }



        public async Task<Folder> CreateFolder(string teamId, string binderId, string folderName, string? parentFolderId)
        {
            var hasParent = !string.IsNullOrEmpty(parentFolderId);
            string response;
            if (hasParent)
            {
                response = await _clientApi.PostAsync($"/teams/{teamId}/folders", new { name = folderName, binderId = binderId, folderId = parentFolderId });
            }
            else
            {
                response = await _clientApi.PostAsync($"/teams/{teamId}/folders", new { name = folderName, binderId = binderId });
            }

            var folders = JsonConvert.DeserializeObject<Folder>(response);

            return folders ?? null;
        }

    }
}
