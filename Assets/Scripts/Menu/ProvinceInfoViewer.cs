using System.Collections.Generic;
using Immerse.BfhClient.Game;
using TMPro;
using UnityEngine;

namespace Immerse.BfhClient
{
    public class ProvinceInfoViewer : MonoBehaviour
    {
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private TextMeshProUGUI provinceNameText;
        [SerializeField] private TextMeshProUGUI provinceInfoText;
        [SerializeField] private ProvinceInfo[] provinceInfos;
        private Dictionary<string, ProvinceInfo> _provinceInfos = new();

        private void Awake()
        {
            infoPanel.SetActive(false);
            foreach (var provinceInfo in provinceInfos)
                _provinceInfos[provinceInfo.ProvinceName] = provinceInfo;
        }

        private void Start()
        {
            RegionSelector.Instance.RegionSelected += HandleRegionSelected;
        }

        private void HandleRegionSelected(Region region)
        {
            Debug.Log("aAAAAA!!!!");
            if (region is null)
            {
                infoPanel.SetActive(false);
                return;
            }
            if (!infoPanel.activeSelf)
                infoPanel.SetActive(true);
            
            if (!_provinceInfos.TryGetValue(region.Name, out var info)) return;
            provinceNameText.text = info.ProvinceName;
            provinceInfoText.text = info.ProvinceInfoText;
        }
    }
}