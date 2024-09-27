using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

public class NormcoreSync : RealtimeComponent<NormcoreSyncModel>
{
    private string lobbyID;

    private void OnDestroy()
    {
        ClearLobbyId();
    }

    private void UpdateLobbyId()
    {
        lobbyID = model.lobbyId;
    }

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

    private void DidLobbyIdChange(NormcoreSyncModel model, string value)
    {
        UpdateLobbyId();
    }

    public void SetLobbyId()
    {
        if (string.IsNullOrEmpty(lobbyID))
        {
            lobbyID = System.Guid.NewGuid().ToString();
            model.lobbyId = lobbyID;

            Debug.LogError("New lobby id is " + model.lobbyId);
        }
    }

    public bool TryGetLobbyId(out string lobbyId)
    {
        lobbyId = model.lobbyId;
        Debug.LogError("Lobby id is already set and is " + lobbyId);

        return !string.IsNullOrEmpty(lobbyId);
    }

    public void ClearLobbyId()
    {
        lobbyID = null;
        model.lobbyId = null;
    }
}
