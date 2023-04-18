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
    public class ContentServices
    {
        private readonly FlorenceApi _clientApi;

        public ContentServices(FlorenceApi clientApi)
        {
            _clientApi = clientApi;
        }

        public async Task<(byte[], string)> DownloadContent(string teamId, string contentId)
        {
            var (response, fileName) = await _clientApi.GetFileAsync($"/teams/{teamId}/contents/{contentId}/file?version=1&includeSignaturePage=false&reduceFileSize=false" );
            return (response,fileName);
        }

        public async Task<string> UploadContent(string teamId, byte[] fileContent, string fileName, string binderId,
            string folderId)
        {
            var response = await _clientApi.PostBinaryAsync($"teams/{teamId}/contents", fileContent, fileName,teamId,binderId,folderId);
            return response;
        }


    }
}
