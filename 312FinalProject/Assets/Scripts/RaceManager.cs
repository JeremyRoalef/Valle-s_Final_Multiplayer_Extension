using System.Collections.Generic;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [SerializeField] private int lastCheckpointIndex = -1;
    [SerializeField] private GameObject player;

    private float lastCheckpointTime;
    private float totalTime;
    private float timeSinceGrounded = 0f;
    private float timeSinceLastCheckpoint = 0f;

    [HideInInspector] public bool raceStarted = false;
    private bool raceFinished = false;
    private bool failTriggered = false;

    private Movement playerMovement;
    public List<Checkpoint> checkpoints;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        playerMovement = player.GetComponent<Movement>();
    }

    void Update()
    {
        // Update timers for checkpoints and UI purposes only if the race has started.
        if (raceStarted)
        {
            UpdateTime();
            UIManager.Instance.UpdateTimers(totalTime, lastCheckpointTime);
        }
    }

    // Logic for when we trigger checkpoints; called by the checkpoint class
    public void CheckpointReached(int checkpointIndex)
    {
        // Ignore if the race hasn't started yet and we didn't start it, or if the race is finished
        if ((!raceStarted && checkpointIndex != 0) || raceFinished) return;

        // Always apply checkpoint logic
        UpdateCheckpoint(checkpointIndex);

        // For medium and hard difficulties, will track timers every 10 track pieces. 
        if (checkpointIndex % 10 == 0 && checkpointIndex > 1)
        {
            lastCheckpointTime = totalTime;  // still store it
            UIManager.Instance.UpdateTimers(totalTime, lastCheckpointTime); // notify UI
        }
    }

    // Start Race method
    public void StartRace()
    {
        raceStarted = true;
        raceFinished = false;
        failTriggered = false;
        playerMovement.StartPlayer();
    }

    // Reset only the race state; for when you fail or win and are in a menu
    public void ResetRaceStateOnly()
    {
        raceStarted = false;
        raceFinished = false;
        failTriggered = false;

        totalTime = 0f;
        timeSinceGrounded = 0f;
        timeSinceLastCheckpoint = 0f;
        lastCheckpointTime = 0f;

        lastCheckpointIndex = -1;

        playerMovement.ResetPlayer();
        playerMovement.StopPlayer();
    }

    // Reset and restart the race; for when you hit retry on the win or lose menu
    public void ResetRaceAndStart()
    {
        ResetRaceStateOnly();
        StartRace();
    }

    // Update the currently passed checkpoint. 
    private void UpdateCheckpoint(int checkpointIndex)
    {
        // If we passed the last checkpoint,
        if (checkpointIndex == checkpoints.Count - 1)
        {
            EndRace();
            UIManager.Instance.ShowWin(totalTime);
            return;
        }

        if (checkpointIndex == 0 && !raceStarted)
        {
            StartRace(); // Only auto-start for first checkpoint if race not started
        }

        timeSinceLastCheckpoint = 0; // Reset time since last checkpoint
        lastCheckpointIndex = checkpointIndex; // Set index of the most recently passed checkpoint to
                                               // the one we just passed
    }

    private void EndRace()
    {
        raceStarted = false;
        raceFinished = true;
    }

    // Upddate total race timers
    private void UpdateTime()
    {
        // Always increment total time and time since last checkpoint
        totalTime += Time.deltaTime;
        timeSinceLastCheckpoint += Time.deltaTime;

        // increment time since grounded only while not grounded
        if (!playerMovement.isGrounded)
            timeSinceGrounded += Time.deltaTime;
        else
            timeSinceGrounded = 0f;


        // Fail condition: if fail hasn't already triggered or you've spent too long mid-air or not 
        // progressing to the next checkpoint, begin fail sequence. 
        // if (!failTriggered && (timeSinceGrounded > 5f || timeSinceLastCheckpoint > 10f))
        // {
        //     failTriggered = true;
        //     raceStarted = false;           // pause race
        //     playerMovement.StopPlayer();
        //     UIManager.Instance.ShowFail(); // show fail overlay
        // }
    }
}