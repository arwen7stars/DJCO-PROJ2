﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Melody : MonoBehaviour {

    private abstract class State
    {
        protected Melody inst;
        protected double dist;
        protected int currentTime;

        public State(Melody inst)
        {
            this.inst = inst;

            Vector2 player = new Vector2(inst.player.transform.position.x, inst.player.transform.position.z);
            Vector2 melodyPos = new Vector2(inst.transform.position.x, inst.transform.position.z);

            dist = Vector2.Distance(player, melodyPos);
            currentTime = (int)Time.timeSinceLevelLoad;
        }

        public virtual void update()
        {
            Vector2 player = new Vector2(inst.player.transform.position.x, inst.player.transform.position.z);
            Vector2 melodyPos = new Vector2(inst.transform.position.x, inst.transform.position.z);

            dist = Vector2.Distance(player, melodyPos);

            currentTime = (int)Time.timeSinceLevelLoad;

            // update volume according to range
            if (inst.gameObject.tag != "Spotlight")
            {
                inst.aSrc.volume = inst.defaultVolume - (float)((dist - inst.maxVolRadius) * (inst.defaultVolume) / (inst.radius - inst.maxVolRadius));
            }
        }
    }

    private class PlayingState : State
    {

        public PlayingState(Melody inst) : base(inst)
        {
            inst.aSrc.Play();
            BGM.StopInstThemes();
        }

        public override void update()
        {
            base.update();

            // if player in range
            if ((dist <= inst.radius && inst.gameObject.tag != "Spotlight") ||
                (inst.gameObject.tag == "Spotlight" && inst.GetComponent<Spotlight>().hasPlayer))
            {
                // if melody is over
                if (!inst.aSrc.isPlaying)
                {
                    // wait for player window
                    inst.state = new WaitingState(inst);
                }
            }
            else
            {
                // stop playing
                inst.state = new StopState(inst);
            }
        }
    }

    private class StopState : State
    {

        public StopState(Melody inst) : base(inst)
        {
            inst.aSrc.Stop();           
        }

        public override void update()
        {
            base.update();

            // if player in range
            if ((dist <= inst.radius && inst.gameObject.tag != "Spotlight") ||
                (inst.gameObject.tag == "Spotlight" && inst.GetComponent<Spotlight>().hasPlayer))
            {
                // wait correct timing
                if (currentTime % 4 == 0)
                {
                    // start playing
                    inst.state = new PlayingState(inst);
                }
            }
        }
    }

    private class WaitingState : State
    {
        private int startTime;

        public WaitingState(Melody inst) : base(inst)
        {
            inst.aSrc.Stop();
            startTime = currentTime;
        }

        public override void update()
        {
            base.update();

            // if player in range
            if ((dist <= inst.radius && inst.gameObject.tag != "Spotlight") ||
                (inst.gameObject.tag == "Spotlight" && inst.GetComponent<Spotlight>().hasPlayer))
            {
                // wait correct timing (min 2s waiting)
                if (currentTime % 4 == 0 && currentTime - startTime > 2)
                {
                    // start playing
                    inst.state = new PlayingState(inst);
                }
            }
            else
            {
                // no need to wait if player not in range
                inst.state = new StopState(inst);
            }
        }
    }

    /* --- Inspector --- */

    /* Menu UI. */
    public InGameMenu menu;
	
	/* Rythem player needs to tap to. */
	public string rythem;

    /* Instruments this melody require to be played. */
    public GameObject[] instruments;

	/* Radius in which melody starts playing. */
    public float radius;

	/* Radius in which the melody plays at max volume. */
	public float maxVolRadius;

	/* Player. */
	public GameObject player;


	/* --- Attributes --- */

	/* Audio Source. */
	protected AudioSource aSrc;

	/* Default max volume. */
	private float defaultVolume;

    /* Current state. */
    private State state;

    /* True if the game is paused, false otherwise. */
    public bool paused;


    /* --- Methods --- */

    /// <summary>
    /// Initialization.
    /// </summary>
    protected void Start()
    {
        aSrc = gameObject.GetComponent<AudioSource>();
		defaultVolume = aSrc.volume;
        paused = false;
        state = new StopState(this);
	}


    /// <summary>
    /// Update once per frame.
    /// </summary>
    protected void Update()
    {
        // if game is paused
        if (menu.isGameStopped())
        {
            // pause audio playback
            if (aSrc.isPlaying)
            {
                paused = true;
                aSrc.Pause();
            }
        }
        else
        {
            // resume audio playback
            if (paused)
            {
                paused = false;
                aSrc.UnPause();
            }

            // update
            state.update();
        }        
    }


    /// <summary>
    /// Called when the player presses a skill and enemy should wait.
    /// </summary>
    public void Wait()
    {
        state = new WaitingState(this);
    }

    public bool IsStopped()
    {
        return (state is StopState);
    }

    // checks if the combination of instruments the player owns atm is valid
    // for this melody
    public bool IsValid()
    {
        if (instruments.Length < 1)
        {
            return true;
        }

        bool pDrums = false;
        bool pGuitar = false;
        bool pPiano = false;

        foreach (GameObject obj in Movement.party)
        {
            if (obj.name == "PartyTambor")
            {
                pDrums = true;
            }
            else if (obj.name == "PartyGuitar")
            {
                pGuitar = true;
            }
            else if (obj.name == "PartyPiano")
            {
                pPiano = true;
            }
        }

        bool mDrums = false;
        bool mGuitar = false;
        bool mPiano = false;

        foreach (GameObject obj in instruments)
        {
            if (obj.name == "PartyTambor")
            {
                mDrums = true;
            }
            else if (obj.name == "PartyGuitar")
            {
                mGuitar = true;
            }
            else if (obj.name == "PartyPiano")
            {
                mPiano = true;
            }
        }

        if (gameObject.tag == "Enemy" || gameObject.tag == "Spotlight" && gameObject.GetComponent<Spotlight>().player.gameObject.name == "Player")
        {
            // check if player owns require inst
            if (mDrums && !pDrums ||
                mGuitar && !pGuitar ||
                mPiano && !pPiano ||
                mGuitar && !mPiano && pGuitar && pPiano ||
                !mGuitar && mPiano && pGuitar && pPiano)
            {
                return false;
            }
        }
        else if (gameObject.tag == "Spotlight")
        {
            if (mGuitar && mPiano)
            {
                return false;
            }
            
            string inFocus = gameObject.GetComponent<Spotlight>().player.gameObject.name;

            if (mDrums && inFocus != "PartyTambor" ||
                mGuitar && inFocus != "PartyGuitar" ||
                mPiano && inFocus != "PartyPiano")
            {
                return false;
            }
        }

        return true;
    }
}
