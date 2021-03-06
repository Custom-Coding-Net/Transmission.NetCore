using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Transmission.NetCore.Client.Models;

namespace Transmission.NetCore.Client.Test
{
    [TestClass]
    public class Method_Tests
    {
        const string HOST = "http://192.168.1.50:9091/transmission/rpc";
        const string SESSION_ID = "";
        string FILE_PATH = System.AppContext.BaseDirectory + "\\Data\\ubuntu-10.04.4-server-amd64.iso.torrent";

        TransmissionClient client = new TransmissionClient(HOST, SESSION_ID);

        #region Torrent Test

        [TestMethod]
        public async Task AddTorrent_Test()
        {
            Assert.IsTrue(File.Exists(FILE_PATH), "Torrent file not found");

            var fstream = File.OpenRead(FILE_PATH);
            byte[] filebytes = new byte[fstream.Length];
            fstream.Read(filebytes, 0, Convert.ToInt32(fstream.Length));

            string encodedData = Convert.ToBase64String(filebytes);

            //The path relative to the server (priority than the metadata)
            //string filename = "/DataVolume/shares/Public/Transmission/torrents/ubuntu-10.04.4-server-amd64.iso.torrent";
            var torrent = new NewTorrent
            {
                //Filename = filename,
                Metainfo = encodedData,
                Paused = false
            };

            var newTorrentInfo = await client.TorrentAddAsync(torrent);

            Assert.IsNotNull(newTorrentInfo);
            Assert.IsTrue(newTorrentInfo.ID != 0);
        }

        [TestMethod]
        public async Task GetTorrentInfo_Test()
        {
            var torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS);

            Assert.IsNotNull(torrentsInfo);
            Assert.IsNotNull(torrentsInfo.TorrentList);
            Assert.IsTrue(torrentsInfo.TorrentList.Any());
        }

        [TestMethod]
        public async Task SetTorrentSettings_Test()
        {
            var torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS);
            var torrentInfo = torrentsInfo.TorrentList.FirstOrDefault();
            Assert.IsNotNull(torrentInfo, "Torrent not found");

            var trackerInfo = torrentInfo.Trackers.FirstOrDefault();
            Assert.IsNotNull(trackerInfo, "Tracker not found");
            var trackerCount = torrentInfo.Trackers.Length;
            TorrentSettings settings = new TorrentSettings()
            {
                IDs = new int[] { torrentInfo.ID },
                TrackerRemove = new int[] { trackerInfo.ID }
            };

            client.TorrentSetAsync(settings);

            torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS, torrentInfo.ID);
            torrentInfo = torrentsInfo.TorrentList.FirstOrDefault();

            Assert.IsFalse(trackerCount == torrentInfo.Trackers.Length);
        }

        [TestMethod]
        public async Task RenamePathTorrent_Test()
        {
            var torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS);
            var torrentInfo = torrentsInfo.TorrentList.FirstOrDefault();
            Assert.IsNotNull(torrentInfo, "Torrent not found");

            var result = await client.TorrentRenamePathAsync(torrentInfo.ID, torrentInfo.Files[0].Name, "test_" + torrentInfo.Files[0].Name);

            Assert.IsNotNull(result, "Torrent not found");
            Assert.IsTrue(result.ID != 0);
        }

        [TestMethod]
        public async Task RemoveTorrent_Test()
        {
            var torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS);
            var torrentInfo = torrentsInfo.TorrentList.FirstOrDefault();
            Assert.IsNotNull(torrentInfo, "Torrent not found");

            client.TorrentRemoveAsync(new int[] { torrentInfo.ID });

            torrentsInfo = await client.TorrentGetAsync(TorrentFields.ALL_FIELDS);

            Assert.IsFalse(torrentsInfo.TorrentList.Any(t => t.ID == torrentInfo.ID));
        }

        #endregion

        #region Session Test

        [TestMethod]
        public async Task SessionGetTest()
        {
            var info = await client.SessionGetAsync();
            Assert.IsNotNull(info);
            Assert.IsNotNull(info.Version);
        }

        [TestMethod]
        public async Task ChangeSessionTest()
        {
            //Get current session information
            var sessionInformation = await client.SessionGetAsync();

            //Save old speed limit up
            var oldSpeedLimit = sessionInformation.SpeedLimitUp;

            //Set new speed limit
            sessionInformation.SpeedLimitUp = 200;

            //Set new session settings
            client.SessionSetAsync(sessionInformation);

            //Get new session information
            var newSessionInformation = await client.SessionGetAsync();

            //Check new speed limit
            Assert.AreEqual(newSessionInformation.SpeedLimitUp, 200);

            //Restore speed limit
            newSessionInformation.SpeedLimitUp = oldSpeedLimit;

            //Set new session settinhs
            client.SessionSetAsync(newSessionInformation);
        }

        #endregion

        #region System
        [TestMethod]
        public async Task PortTest_Test()
        {
            await client.PortTestAsync();
            //Any result is acceptable
        }
        #endregion
    }
}
