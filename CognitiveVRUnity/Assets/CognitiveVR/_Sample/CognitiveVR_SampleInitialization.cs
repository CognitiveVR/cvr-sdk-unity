using UnityEngine;
using CognitiveVR;

///===============================================================
/// This script is taken from https://github.com/CognitiveVR/cvr-sdk-unity
/// Simply placing this in your scene will capture device and user data, such as GPU,CPU,OS,RAM
///===============================================================

public class CognitiveVR_SampleInitialization : MonoBehaviour
{
    public string CustomerID = "companyname1234-productname-test";

    void Start ()
    {
        CognitiveVR.InitParams initParams = CognitiveVR.InitParams.create
        (
            customerId: CustomerID // contact CognitiveVR if you do not have a customer id yet

            //if you are using the SteamVR plugin, this will also automatically record the player's room size

            //if you are using Unity 5.4 beta, this will also automatically record the player's HMD model
        );


        CognitiveVR.Core.init(initParams, delegate (Error initError)
        {
            // let application know that CognitiveVR is ready
            Debug.Log("CognitiveVR Initialize. Result: " + initError);
            if (initError != Error.Success) { return; }


            //USER STEAM ID
            //if you are using steamworks.net (https://steamworks.github.io/installation/) you can use this code to pass in your user's steam id
            //Steamworks.SteamAPI.Init(); //doesn't have to be called here, but Steamworks must be Initialized before you call GetSteamID()
            //EntityInfo user = CognitiveVR.EntityInfo.createUserInfo(Steamworks.SteamUser.GetSteamID().ToString());
            //Core.registerUser(user, delegate (Error error) { });



            //this is best done on a 'new game' button from your menu. maybe every time the player visits the main menu
            CognitiveVR.Plugins.Session.Transaction().begin();


            //this kind of thing should be called when a player begins a new level. starting this allows all other transactions to exist in a context
            //for example: can be used to 'slice' data on the dashboard to see how many enemies a player killed in each level
            //CognitiveVR.Instrumentation.Transaction("level").begin();


            //make sure to end you level correctly
            //CognitiveVR.Instrumentation.Transaction("level").end();


            //make sure to end a session when the player quits to the main menu
            //CognitiveVR.Plugins.Session.Transaction().end();
        });
    }
}