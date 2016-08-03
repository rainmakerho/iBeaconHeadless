using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth.Advertisement;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;

namespace iBeacon
{
    public sealed class StartupTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Create the deferral by requesting it from the task instance.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            try
            {
                //先讀取 beaconData 設定的 Beacon 資訊
                var beaconData = await GetBeaconData();
                // 開始廣播 iBeacon 的廣告訊息
                PublishiBeacon(beaconData);
            }catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            
            
            // Once the asynchronous method(s) are done, close the deferral.
            // 不要 Call Complete 就不會停止
            //deferral.Complete();
        }

        /// <summary>
        /// 取得 beacon 的資訊
        /// </summary>
        /// <returns></returns>
        private async Task<byte[]> GetBeaconData()
        {
            string fileContent = await ReadBeaconDataFromFile();
            dynamic beacon = JsonConvert.DeserializeObject(fileContent);
            StringBuilder beaconData = new StringBuilder();
            beaconData.Append((string)beacon.manufacturerId);
            beaconData.Append(((string)beacon.uuid).Replace("-",string.Empty));
            beaconData.Append((string)beacon.major);
            beaconData.Append((string)beacon.minor);
            beaconData.Append((string)beacon.txPower);
            string beaconDataStr = beaconData.ToString();
            var result = Enumerable.Range(0, beaconDataStr.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(beaconDataStr.Substring(x, 2), 16))
                .ToArray();

            return result;
        }

        /// <summary>
        /// 讀取 beaconData.json 的內容
        /// </summary>
        /// <returns></returns>
        private async Task<string> ReadBeaconDataFromFile()
        {
            var fileName = @"beaconData.json";
            StorageFolder folder;
            StorageFile file;
            try
            {
                //先在 LocalFolder 裡找
                folder = ApplicationData.Current.LocalFolder;
                file = await folder.GetFileAsync(fileName);
            }
            catch (Exception ex)
            {

                //找不到檔案，所以取預設的地方
                folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                file = await folder.GetFileAsync(fileName);
                Debug.WriteLine(ex.ToString());
            }
            var fileContent = await FileIO.ReadTextAsync(file);
            return fileContent;
        }

        BluetoothLEAdvertisementPublisher _blePublisher;
        private void PublishiBeacon(byte[] dataArray)
        {
            var manufactureData = new BluetoothLEManufacturerData();
            //0x004C	Apple, Inc.
            manufactureData.CompanyId = 0x004c;
            //using System.Runtime.InteropServices.WindowsRuntime;
            manufactureData.Data = dataArray.AsBuffer();
            _blePublisher = new BluetoothLEAdvertisementPublisher();
            _blePublisher.Advertisement.ManufacturerData.Add(manufactureData);
            //開始發佈
            _blePublisher.Start();
        }
    }
}
