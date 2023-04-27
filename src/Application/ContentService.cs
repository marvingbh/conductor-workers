using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using APIClient;
using Conductor.Client.Models;
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
            try
            {
                var (response, fileName) = await _clientApi.GetFileAsync($"/teams/{teamId}/contents/{contentId}/file?version=1&includeSignaturePage=false&reduceFileSize=false");
                return (response, fileName);
            }
            catch (Exception e)
            {
                throw new Exception($"Error DownloadContent 'team: {teamId} contentId: {contentId}': {e.Message}",e);
            }

        }

        public async Task<string> UploadContent(string teamId, byte[] fileContent, string fileName, string binderId,
            string folderId)
        {
            try
            {
                var response = await _clientApi.PostBinaryAsync($"teams/{teamId}/contents", fileContent, fileName, teamId, binderId, folderId);
                return response;
            }
            catch (Exception e)
            {
                throw new Exception($"Error Uploading 'team: {teamId} binder: {binderId} file:{fileName}': {e.Message}", e);

            }

        }


    }
}
