using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Reflection;

// BepInEx plugin attribute: sets up the plugin's identity
[BepInPlugin("com.pulsion.SpawnFrisbeeInAirport", "Airport Frisbee Spawner", "1.0.0")]
public class SpawnFrisbeePlugin : BaseUnityPlugin
{
    // Subscribe to the sceneLoaded event when the plugin is enabled
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unsubscribe from the sceneLoaded event when the plugin is disabled
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called whenever a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only run our logic if the Airport scene is loaded
        if (scene.name == "Airport")
        {
            // Start the coroutine that waits for the player to have the Passport and then attaches the component
            StartCoroutine(WaitForItemAndAttach());
        }
    }

    // Checks if the player has Passport with ushort ID 59 in any slot or temp slot
    private bool IsHoldingPassport(Player player)
    {
        // Check all regular item slots
        foreach (var slot in player.itemSlots)
        {
            if (!slot.IsEmpty() && slot.prefab.itemID == 59)
                return true;
        }

        // Check the temporary slot (e.g., for items being moved)
        var temp = player.tempFullSlot;
        if (!temp.IsEmpty() && temp.prefab.itemID == 59)
            return true;

        // Item not found
        return false;
    }

    // Coroutine that waits for the player to have the Passport, then adds the component to PassportSpawner
    private IEnumerator WaitForItemAndAttach()
    {
        // Wait until the local character is available (player has loaded in)
        while (!Character.localCharacter)
            yield return null;

        // Get the local player and their character
        Player localPlayer = Player.localPlayer;
        Character character = localPlayer.character;

        // Wait until the player is holding the Passport in any slot
        while (!IsHoldingPassport(localPlayer))
            yield return new WaitForSeconds(0.2f); // Check 5 times per second

        // Now wait for the PassportSpawner GameObject to appear in the scene (up to 5 seconds)
        GameObject spawner = null;
        float timer = 0f;
        while (spawner == null && timer < 5f)
        {
            spawner = GameObject.Find("PassportSpawner");
            timer += Time.deltaTime;
            yield return null;
        }

        // If found, and the component isn't already attached, add it
        if (spawner != null && spawner.GetComponent<SpawnFrisbeeInHand>() == null)
        {
            var frisbee = spawner.AddComponent<SpawnFrisbeeInHand>();
            frisbee.Logger = Logger; // Pass the logger for logging inside the component
            Logger.LogInfo("Added SpawnFrisbeeInHand to PassportSpawner after Passport found.");
        }
        else if (spawner == null)
        {
            Logger.LogError("PassportSpawner not found.");
        }
    }
}

// This component, when added to PassportSpawner, will spawn a Frisbee in the player's hand
public class SpawnFrisbeeInHand : MonoBehaviour
{
    public BepInEx.Logging.ManualLogSource Logger { get; set; }

    // Unity coroutine that runs when the component is enabled
    private IEnumerator Start()
    {
        // Wait until the local character exists
        while (!Character.localCharacter)
        {
            yield return null;
        }

        // Wait a bit longer to ensure the scene and references are fully set up
        yield return new WaitForSeconds(1.5f);

        // Check that the items reference exists
        if (Character.localCharacter?.refs?.items == null)
        {
            Logger?.LogError("Character or items reference is null.");
            yield break;
        }

        // Get the items object from the character
        var items = Character.localCharacter.refs.items;

        // Use reflection to get the private SpawnItemInHand method
        MethodInfo spawnItemInHand = typeof(CharacterItems).GetMethod("SpawnItemInHand", BindingFlags.Instance | BindingFlags.NonPublic);

        if (spawnItemInHand != null)
        {
            // Call the method to spawn a Frisbee in the player's hand
            spawnItemInHand.Invoke(items, new object[] { "Frisbee" });
            Logger?.LogInfo("Spawned Frisbee in hand!");
        }
        else
        {
            Logger?.LogError("Could not find SpawnItemInHand method.");
        }
    }
}