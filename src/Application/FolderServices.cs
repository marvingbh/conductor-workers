using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using APIClient;
using Newtonsoft.Json;
using Workers.Dtos;

namespace Workers.Application
{
    public class FolderServices
    {
        private readonly FlorenceApi _clientApi;

        public FolderServices(FlorenceApi clientApi)
        {
            _clientApi = clientApi;
        }

        public async Task<Folders[]?> GetFoldersAndContents(string teamId)
        {
            var response = await _clientApi.GetAsync($"/teams/{teamId}/folders", "");
            var folders = JsonConvert.DeserializeObject<Folders[]>(response);

            if (folders == null) return null;

            foreach (var folder in folders)
            {
                var contentJson = await _clientApi.GetAsync($"/teams/{teamId}/folders/{folder.id}/contents", "");
                var content = JsonConvert.DeserializeObject<Content[]>(contentJson);
                Console.WriteLine(contentJson);
                folder.Contents = new List<Content>();
                folder.Contents.AddRange(content ?? Array.Empty<Content>());
            }

            return folders;

        }


    }
}
