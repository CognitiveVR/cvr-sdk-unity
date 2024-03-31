using Normal.Realtime;
using Cognitive3D;

public class NormCoreRoomModelSync : RealtimeComponent<NormCoreRoomModel>
{
    public void UpdateLobbyId(string id)
    {
        if (model == null) { return; } // model not yet available
        if (!string.IsNullOrEmpty(model.lobbyId)) { return; } // has already been set
        model.lobbyId = id;
    }

    protected override void OnRealtimeModelReplaced(NormCoreRoomModel previousModel, NormCoreRoomModel currentModel)
    {
        base.OnRealtimeModelReplaced(previousModel, currentModel);
        if (string.IsNullOrEmpty(currentModel.lobbyId))
        {
            GenerateLobbyIdAndUpdateModel();
            return;
        }
        else
        {
            Cognitive3D_Manager.SetSessionProperty("c3d.app.multiplayer.lobbyId", currentModel.lobbyId);
            return;
        }
    }

    public string GetLobbyId()
    {
        if (model == null) { return "model null"; }
        if (string.IsNullOrEmpty(model.lobbyId)) { return "empty string"; }
        else return model.lobbyId;
    }

    private void GenerateLobbyIdAndUpdateModel()
    {
        string id = System.Guid.NewGuid().ToString();
        UpdateLobbyId(id);
        Cognitive3D_Manager.SetSessionProperty("c3d.app.multiplayer.lobbyId", id);
    }
}
