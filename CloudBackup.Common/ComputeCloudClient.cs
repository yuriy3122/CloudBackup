using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CloudBackup.Common
{
    public class ComputeCloudClient : CloudClient, IComputeCloudClient
    {
        private readonly string _cloudProviderUrl = "nv";

        public ComputeCloudClient(CloudCredentials credentials) : base(credentials)
        {
        }

        public async Task<InstanceList?> GetInstanceList(string folderId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var nextPageToken = string.Empty;
            var instanceList = new List<Instance>();
            var baseUrl = $"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances?folderId={folderId}";

            do
            {
                var url = string.IsNullOrEmpty(nextPageToken) ? baseUrl : $"{baseUrl}&pageToken={nextPageToken}";
                var listInstanceResponse = await httpClient.GetAsync(new Uri(url));
                var listInstanceContent = await listInstanceResponse.Content.ReadAsStringAsync();
                var listInstanceResult = JsonConvert.DeserializeObject<InstanceList>(listInstanceContent);

                if (listInstanceResult?.Instances != null)
                {
                    instanceList.AddRange(listInstanceResult.Instances);
                }

                nextPageToken = listInstanceResult?.NextPageToken;
            }
            while (!string.IsNullOrEmpty(nextPageToken));

            return new InstanceList() { Instances = instanceList };
        }

        public async Task<DiskSnapshot?> CreateSnapshot(string folderId, string diskId)
        {
            await WaitForDiskSnapshotsToComplete(folderId, diskId);

            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var snapshotDescription = Guid.NewGuid().ToString().Replace("-", "");
            var createSnapshotRequest = JsonConvert.SerializeObject(new { folderId, diskId, description = snapshotDescription });
            var createSnapshotRequestContent = new StringContent(createSnapshotRequest, Encoding.UTF8, "application/json");
            var createSnapshotResponse = await httpClient.PostAsync(
                new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/snapshots"), createSnapshotRequestContent);

            if (!createSnapshotResponse.IsSuccessStatusCode)
            {
                throw new Exception(createSnapshotResponse.ReasonPhrase);
            }

            bool complete = false;
            int retryCount = 0;
            var nextPageToken = string.Empty;
            DiskSnapshot? diskSnapshot = null;

            await Task.Delay(5000);

            do
            {
                var snapshots = new List<DiskSnapshot>();

                do
                {
                    var listSnapshotResponse = await httpClient.GetAsync(
                        new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/snapshots?folderId={folderId}"));

                    DiskSnapshotList? listSnapshotResult = null;

                    if (listSnapshotResponse.IsSuccessStatusCode)
                    {
                        var listSnapshotContent = await listSnapshotResponse.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(listSnapshotContent))
                        {
                            try
                            {
                                listSnapshotResult = JsonConvert.DeserializeObject<DiskSnapshotList>(listSnapshotContent);

                                if (listSnapshotResult?.Snapshots != null)
                                {
                                    snapshots.AddRange(listSnapshotResult.Snapshots);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                    nextPageToken = listSnapshotResult?.NextPageToken;
                }
                while (!string.IsNullOrEmpty(nextPageToken));

                diskSnapshot = snapshots.SingleOrDefault(x => x.Description == snapshotDescription);

                complete = diskSnapshot?.Status == "READY";

                if (!complete)
                {
                    retryCount++;
                    await Task.Delay(5000);
                }

                if (retryCount > 720)
                {
                    throw new TimeoutException("Error creating snapshot");
                }
            }
            while (!complete);

            return diskSnapshot;
        }

        private async Task WaitForDiskSnapshotsToComplete(string folderId, string diskId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            bool complete = false;
            int retryCount = 0;
            var nextPageToken = string.Empty;

            do
            {
                var snapshots = new List<DiskSnapshot>();

                do
                {
                    var listSnapshotResponse = await httpClient.GetAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/snapshots?folderId={folderId}"));
                    var listSnapshotContent = await listSnapshotResponse.Content.ReadAsStringAsync();
                    var listSnapshotResult = JsonConvert.DeserializeObject<DiskSnapshotList>(listSnapshotContent);

                    if (listSnapshotResult?.Snapshots != null)
                    {
                        snapshots.AddRange(listSnapshotResult.Snapshots);
                    }

                    nextPageToken = listSnapshotResult?.NextPageToken;
                }
                while (!string.IsNullOrEmpty(nextPageToken));

                complete = !snapshots.Any(x => x.SourceDiskId == diskId && x.Status == "CREATING");

                if (!complete)
                {
                    retryCount++;
                    await Task.Delay(5000);
                }

                if (retryCount > 720)
                {
                    throw new TimeoutException("Error creating snapshot");
                }
            }
            while (!complete);
        }

        public async Task DeleteSnapshot(string snapshotId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var deleteSnapshotResponse = await httpClient.DeleteAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/snapshots/{snapshotId}"));

            var deleteSnapshotResponseContent = await deleteSnapshotResponse.Content.ReadAsStringAsync();
            var deleteSnapshotResponseeResult = JsonConvert.DeserializeObject<DeleteSnapshotResponse>(deleteSnapshotResponseContent);

            if (deleteSnapshotResponseeResult?.Error != null)
            {
                throw new Exception($"Failed to delete snapshot: {deleteSnapshotResponseeResult.Error.Message}");
            }
        }

        public async Task<DiskDescriptionList?> GetDiskList(string folderId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var nextPageToken = string.Empty;
            var instanceList = new List<DiskDescription>();
            var baseUrl = $"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/disks?folderId={folderId}";

            do
            {
                var url = string.IsNullOrEmpty(nextPageToken) ? baseUrl : $"{baseUrl}&pageToken={nextPageToken}";
                var listDiskResponse = await httpClient.GetAsync(new Uri(url));
                var listDiskContent = await listDiskResponse.Content.ReadAsStringAsync();
                var listDiskResult = JsonConvert.DeserializeObject<DiskDescriptionList>(listDiskContent);

                if (listDiskResult?.Disks != null)
                {
                    instanceList.AddRange(listDiskResult.Disks);
                }

                nextPageToken = listDiskResult?.NextPageToken;
            }
            while (!string.IsNullOrEmpty(nextPageToken));

            return new DiskDescriptionList() { Disks = instanceList };
        }

        public async Task<CreateResourceResponse?> CreateDisk(CreateDiskRequest request)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var createDiskRequest = JsonConvert.SerializeObject(new 
                {
                    blockSize = request.BlockSize,
                    description = request.Description,
                    folderId = request.FolderId,
                    name = request.Name,
                    size = request.Size,
                    snapshotId = request.SnapshotId,
                    typeId = request.TypeId,
                    zoneId = request.ZoneId
                });

            var createDiskRequestContent = new StringContent(createDiskRequest, Encoding.UTF8, "application/json");

            var createDiskResponse = await httpClient.PostAsync(new Uri("https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/disks"), createDiskRequestContent);

            var createDiskResponseContent = await createDiskResponse.Content.ReadAsStringAsync();

            if (!createDiskResponse.IsSuccessStatusCode)
            {
                throw new Exception(createDiskResponseContent);
            }

            var createDiskResponseResult = JsonConvert.DeserializeObject<CreateResourceResponse>(createDiskResponseContent);

            bool complete = false;
            int retryCount = 0;

            await Task.Delay(5000);

            do
            {
                var diskList = await GetDiskList(request.FolderId);

                if (diskList != null && createDiskResponseResult != null)
                {
                    var disk = diskList.Disks?.FirstOrDefault(x => x.Name == request.Name);
                    complete = disk?.Status == "READY";
                    createDiskResponseResult.Id = disk?.Id;
                }

                if (!complete)
                {
                    retryCount++;
                    await Task.Delay(5000);
                }

                if (retryCount > 720)
                {
                    throw new TimeoutException("Error creating snapshot");
                }
            }
            while (!complete);

            return createDiskResponseResult;
        }

        public async Task<CreateResourceResponse?> CreateInstance(CreateInstanceRequest request)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var createInstanceRequest = JsonConvert.SerializeObject(request, new JsonSerializerSettings() { 
                ContractResolver = new CamelCasePropertyNamesContractResolver() });

            var createInstanceRequestContent = new StringContent(createInstanceRequest, Encoding.UTF8, "application/json");
            var createInstanceResponse = await httpClient.PostAsync(
                new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances"), createInstanceRequestContent);

            var createInstanceResponseContent = await createInstanceResponse.Content.ReadAsStringAsync();

            if (!createInstanceResponse.IsSuccessStatusCode)
            {
                throw new Exception(createInstanceResponseContent);
            }

            var createInstanceResponseResult = JsonConvert.DeserializeObject<CreateResourceResponse>(createInstanceResponseContent);

            bool running = false;
            int retryCount = 0;

            await Task.Delay(10 * 1000);

            do
            {
                var folderId = request.FolderId ?? string.Empty;
                var instanceList = await GetInstanceList(folderId);

                if (instanceList != null && createInstanceResponseResult != null)
                {
                    var instance = instanceList.Instances?.FirstOrDefault(x => x.Name == request.Name);
                    running = instance?.Status == "RUNNING";
                    createInstanceResponseResult.Id = instance?.Id;
                }

                if (!running)
                {
                    retryCount++;
                    await Task.Delay(5000);
                }

                if (retryCount > 720)
                {
                    throw new TimeoutException("Error creating snapshot");
                }
            }
            while (!running);

            return createInstanceResponseResult;
        }

        public async Task<AttachDiskResponse?> AttachDisk(string instanceId, string folderId, AttachDiskRequest request)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var attachDiskRequest = JsonConvert.SerializeObject(new { attachedDiskSpec = request }, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var attachDiskRequestContent = new StringContent(attachDiskRequest, Encoding.UTF8, "application/json");

            var attachDiskResponse = await httpClient.PostAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances/{instanceId}:attachDisk"), attachDiskRequestContent);

            var attachDiskResponseContent = await attachDiskResponse.Content.ReadAsStringAsync();

            if (!attachDiskResponse.IsSuccessStatusCode)
            {
                throw new Exception(attachDiskResponseContent);
            }

            var attachDiskResponseResult = JsonConvert.DeserializeObject<AttachDiskResponse>(attachDiskResponseContent);

            bool complete = false;
            int retryCount = 0;

            do
            {
                await Task.Delay(1000);

                var diskList = await GetDiskList(folderId);

                if (diskList != null)
                {
                    var disk = diskList.Disks?.SingleOrDefault(x => x.Id == request.DiskId);

                    if (disk != null && disk.InstanceIds != null)
                    {
                        complete = disk.InstanceIds.Contains(instanceId);
                    }
                }

                if (!complete)
                {
                    retryCount++;
                }

                if (retryCount > 300)
                {
                    throw new TimeoutException("Error attaching disk");
                }
            }
            while (!complete);

            return attachDiskResponseResult;
        }

        public async Task<DetachDiskResponse?> DetachDisk(string instanceId, string folderId, DetachDiskRequest request)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var detachRequest = JsonConvert.SerializeObject(new { diskId = request.DiskId });
            var detachDiskRequestContent = new StringContent(detachRequest, Encoding.UTF8, "application/json");

            var detachResponse = await httpClient.PostAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances/{instanceId}:detachDisk"), detachDiskRequestContent);

            var detachResponseContent = await detachResponse.Content.ReadAsStringAsync();

            if (!detachResponse.IsSuccessStatusCode)
            {
                throw new Exception(detachResponseContent);
            }

            var responseResult = JsonConvert.DeserializeObject<DetachDiskResponse>(detachResponseContent);

            bool complete = false;
            int retryCount = 0;

            do
            {
                await Task.Delay(1000);

                var diskList = await GetDiskList(folderId);

                if (diskList != null)
                {
                    var disk = diskList.Disks?.SingleOrDefault(x => x.Id == request.DiskId);

                    if (disk != null && (disk.InstanceIds == null || !disk.InstanceIds.Contains(instanceId)))
                    {
                        complete = true;
                    }
                }

                if (!complete)
                {
                    retryCount++;
                }

                if (retryCount > 300)
                {
                    throw new TimeoutException("Error detaching disk");
                }
            }
            while (!complete);

            return responseResult;
        }

        public async Task<DeleteDiskResponse?> DeleteDisk(string diskId)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var deleteResponse = await httpClient.DeleteAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/disks/{diskId}"));

            var deleteResponseContent = await deleteResponse.Content.ReadAsStringAsync();

            if (!deleteResponse.IsSuccessStatusCode)
            {
                throw new Exception(deleteResponseContent);
            }

            var responseResult = JsonConvert.DeserializeObject<DeleteDiskResponse>(deleteResponseContent);

            return responseResult;
        }

        public async Task StartInstances(string folderId, string[] instanceIds)
        {
            await InstancesExecuteOperation(folderId, instanceIds, "start");
        }

        public async Task StopInstances(string folderId, string[] instanceIds)
        {
            await InstancesExecuteOperation(folderId, instanceIds, "stop");
        }

        public async Task RestartInstances(string folderId, string[] instanceIds)
        {
            await InstancesExecuteOperation(folderId, instanceIds, "restart");
        }

        public async Task DeleteInstances(string[] instanceIds)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            foreach (var instanceId in instanceIds)
            {
                var response = await httpClient.DeleteAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances/{instanceId}"));

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(responseContent);
                }

                var responseResult = JsonConvert.DeserializeObject<CommonResponse>(responseContent);

                if (responseResult?.Error != null)
                {
                    throw new InvalidOperationException(responseResult?.Error.Message);
                }
            }
        }

        private async Task InstancesExecuteOperation(string folderId, string[] instanceIds, string operation)
        {
            using var httpClient = new HttpClient();

            await AddAuthorizationHeaders(httpClient);

            var currentInstanceId = string.Empty;
#if (DEBUG)
            currentInstanceId = "epdin6dcm3kiqfmk7t7d";
#else
            currentInstanceId = InstanceMetadata.GetInstanceId();
#endif
            foreach (var instanceId in instanceIds)
            {
                if (instanceId == currentInstanceId)
                {
                    continue;
                }

                var response = await httpClient.PostAsync(new Uri($"https://compute.api.cloud.{_cloudProviderUrl}.net/compute/v1/instances/{instanceId}:{operation}"), null);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(responseContent);
                }

                var responseResult = JsonConvert.DeserializeObject<CommonResponse>(responseContent);

                if (responseResult?.Error != null)
                {
                    throw new InvalidOperationException(responseResult?.Error.Message);
                }
            }

            int count = 0;
            int retryCount = 0;

            await Task.Delay(10 * 1000);

            do
            {
                var instanceList = await GetInstanceList(folderId);

                if (instanceList != null)
                {
                    string status = string.Empty;

                    switch (operation)
                    {
                        case "start":
                        case "restart":
                            status = "RUNNING";
                            break;
                        case "stop":
                            status = "STOPPED";
                            break;
                    }

                    count = instanceList.Instances?.Count(x => instanceIds.Contains(x.Id) && x.Status == status) ?? 0;
                }

                retryCount++;
                await Task.Delay(5000);

                if (retryCount > 720)
                {
                    throw new TimeoutException("Operation error instances");
                }
            }
            while (count < instanceIds.Length);
        }
    }
}