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

    public string SetLobbyId()
    {
        if (string.IsNullOrEmpty(lobbyID))
        {
            lobbyID = System.Guid.NewGuid().ToString();
            model.lobbyId = lobbyID;
        }

        return model.lobbyId;
    }

    public bool TryGetLobbyId(out string lobbyId)
    {
        lobbyId = model.lobbyId;

        return !string.IsNullOrEmpty(lobbyId);
    }

    public void ClearLobbyId()
    {
        lobbyID = null;
        model.lobbyId = null;
    }
}
