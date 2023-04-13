using Immerse.BfhClient.Api;
using Immerse.BfhClient.Api.GameTypes;
using Immerse.BfhClient.Api.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfhClient.Game
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        private Dictionary<Region, Unit> spawnOrders = new();
        public void SpawnOrder(Region region, Unit unit)
        {
            spawnOrders.Add(region, unit);
        }

        private void Awake()
        {
            Instance = this;
        }
        public void SendOrders()
        {
            List<Order> orders = new();
            foreach (Region r in spawnOrders.Keys)
            {
                var troop = spawnOrders[r];
                Order order = new()
                {
                    Type = OrderType.Build,
                    Player = "Joe",
                    Origin = r.Name,
                    Build = troop.Name
                };
                orders.Add(order);
            }

            SubmitOrdersMessage message = new SubmitOrdersMessage
            {
                Orders = orders
            };
            ApiClient.Instance.SendServerMessage(message);
        }
    }
}
