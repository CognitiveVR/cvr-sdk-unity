using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if COGNITIVE3D_INCLUDE_NORMCORE
using Normal.Realtime;

namespace Cognitive3D
{
    // This class that manages syncing data across multiple clients in the Normcore multiplayer environment.
    // It handles the assignment, retrieval, and updates of the lobby ID.
    public class NormcoreSync : RealtimeComponent<NormcoreSyncModel>
    {
        private string lobbyID;

        // Cleans up the lobby ID when the object is destroyed.
        private void OnDestroy()
        {
            ClearLobbyId();
        }

        // Updates the local lobby ID from the model.
        private void UpdateLobbyId()
        {
            lobbyID = model.lobbyId;
        }

        // Handles model replacement by updating lobby ID
        protected override void OnRealtimeModelReplaced(NormcoreSyncModel previousModel, NormcoreSyncModel currentModel)
        {
            if (previousModel != null)
            {
                previousModel.lobbyIdDidChange -= DidLobbyIdChange;
            }

            if ( currentModel != null)
            {
                if (currentModel.isFreshModel)
                {
                    currentModel.lobbyId = lobbyID;
                }

                UpdateLobbyId();

                currentModel.lobbyIdDidChange += DidLobbyIdChange;
            }
        }

        // Called when the lobby ID changes, triggers an update of the lobby ID.
        private void DidLobbyIdChange(NormcoreSyncModel model, string value)
        {
            UpdateLobbyId();
        }

        // Sets a new lobby ID if one is not already present, then assigns it to the model.
        internal string SetLobbyId()
        {
            if (string.IsNullOrEmpty(lobbyID))
            {
                lobbyID = System.Guid.NewGuid().ToString();
                model.lobbyId = lobbyID;
            }

            return model.lobbyId;
        }

        // Attempts to retrieve the lobby ID from the model and returns whether it was successful.
        internal bool TryGetLobbyId(out string lobbyId)
        {
            lobbyId = model.lobbyId;

            return !string.IsNullOrEmpty(lobbyId);
        }

        // Clears the local and model-stored lobby ID.
        internal void ClearLobbyId()
        {
            lobbyID = null;
            model.lobbyId = null;
        }
    }
}
#endif
