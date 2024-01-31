using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Cognitive3D.Components;

namespace Cognitive3D
{
    // Can't inherit multiple classes: https://forum.unity.com/threads/multiple-inheritance-implementation-alternative.367802/
    public class PhotonMultiplayer : MonoBehaviourPunCallbacks
    {
        private int playerPhotonActorNumber;
        private string photonRoomName;

        private void Start()
        {

        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            playerPhotonActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonRoomName = PhotonNetwork.CurrentRoom.Name;
            Cognitive3D_Manager.SetSessionProperty("Player Photon Actor Number", playerPhotonActorNumber);
            Cognitive3D_Manager.SetSessionProperty("Photon Room Name", photonRoomName);
            new CustomEvent("Player created room")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .Send();


        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            playerPhotonActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonRoomName = PhotonNetwork.CurrentRoom.Name;
            Cognitive3D_Manager.SetSessionProperty("Player Photon Actor Number", playerPhotonActorNumber);
            Cognitive3D_Manager.SetSessionProperty("Photon Room Name", photonRoomName);
            if (PhotonNetwork.CurrentRoom != null && photonRoomName != null)
            {
                new CustomEvent("Player joined room")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .Send();
            }
        }

    }

}