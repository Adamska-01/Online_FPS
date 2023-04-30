using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// --------------------------------------------------------------------------
// CLASS	:	AudioLayer
// DESC		:	Just a data container that contains the information about the 
//              current state of each layer that is being managed by the 
//              'LayeredAudioSource'
// --------------------------------------------------------------------------
public class AudioLayer
{
    public AudioClip clip = null;
    public AudioCollection collection = null;
    public int bank = -1;
    public bool looping = true;
    public bool muted = false;
    public float time = 0.0f;
    public float duration = 0.0f;
}

public interface ILayeredAudioSource
{
    bool Play(AudioCollection _collection, int _bank, int _layer, bool _looping = true);
    void Stop(int _layerIndex);
    void Mute(int _layerIndex, bool _mute);
    void Mute(bool _mute); //Mute all layers
}


// --------------------------------------------------------------------------
// CLASS	:	LayeredAudioSource
// DESC		:	Allows to give an audio source to a specific animation layer
//              so that it can play/stop/resume clips without any other layers
//              trying to overlap another clip.      
// --------------------------------------------------------------------------
public class LayeredAudioSource : ILayeredAudioSource
{
    //Private
    private AudioSource audioSrc = null;
    private List<AudioLayer> audioLayers = new List<AudioLayer>();
    private int activeLayer = -1;

    //Properties
    public AudioSource AudioSrc { get { return audioSrc; } }


    //Allocates the layer stack
    public LayeredAudioSource(AudioSource source, int layers)
    {
        if(source != null && layers > 0)
        {
            //Assign audio source to this layer stack
            audioSrc = source;

            //Create the requested number of layers 
            for (int i = 0; i < layers; i++)
            {
                AudioLayer newLayer = new AudioLayer();
                newLayer.clip = null;
                newLayer.collection = null;
                newLayer.bank = 0;
                newLayer.looping = false;
                newLayer.muted = false;
                newLayer.time = 0.0f;
                newLayer.duration = 0.0f;

                audioLayers.Add(newLayer);
            }
        }
    }


    //ILayeredAudioSource
    public bool Play(AudioCollection _collection, int _bank, int _layer, bool _looping = true)
    {
        if (_layer >= audioLayers.Count) //Check layer exists/in range
            return false;

        AudioLayer audioLayer = audioLayers[_layer];

        if (audioLayer == null)
            throw new System.NullReferenceException(); 

        //Check if already playing (no need to do anything)
        if (audioLayer.collection == _collection && audioLayer.looping == _looping && _bank == audioLayer.bank)
            return true;

        //Configure
        audioLayer.clip = null;
        audioLayer.collection = _collection;
        audioLayer.bank = _bank;
        audioLayer.looping = _looping;
        audioLayer.time = 0.0f;
        audioLayer.duration = 0.0f;
        audioLayer.muted = false;

        return true;
    }

    public void Stop(int _layerIndex)
    {
        if (_layerIndex >= audioLayers.Count) //Check layer exists/in range
            return;

        AudioLayer layer = audioLayers[_layerIndex];

        if (layer == null)
            throw new System.NullReferenceException();

        //Stop (time has reached duration, not looping)
        layer.looping = false;
        layer.time = layer.duration;
    }

    public void Mute(int _layerIndex, bool _mute)
    {
        if (_layerIndex >= audioLayers.Count) //Check layer exists/in range
            return;

        AudioLayer layer = audioLayers[_layerIndex];

        if (layer == null)
            throw new System.NullReferenceException();

        layer.muted = _mute;
    }

    public void Mute(bool _mute)
    {
        for (int i = 0; i < audioLayers.Count; i++)
        {
            Mute(i, _mute);
        }
    }


    //Update the time of all layered clips and makes sure that the audio source
    //is playing the clip on the highest layer
    public void Update()
    {
        //Record the highest layer with a clip assigned (and playing)
        int newActiveLayer = -1;
        bool refreshAudioSource = false;
        //Update the stack each frame by iterating the layers (Working backwards)
        for (int i = audioLayers.Count - 1; i >= 0; i--)
        {
            //Layer being processed
            AudioLayer layer = audioLayers[i];

            //Ignore unassigned layers
            if (layer.collection == null) 
                continue;

            //Update the internal playhead of the layer		
            layer.time += Time.deltaTime;

            //If it has exceeded its duration then we need to take action
            if (layer.time > layer.duration)
            {
                //If its a looping sound OR the first time we have set up this layer
                //we need to assign a new clip from the pool assigned to this layer
                if (layer.looping || layer.clip == null)
                {
                    //Fetch a new clip from the pool
                    AudioClip clip = layer.collection[layer.bank];

                    //Calculate the play position based on the time of the layer and store duration
                    if (clip == layer.clip) //Wrap
                    {
                        layer.time = layer.time % layer.clip.length;
                    }
                    else
                    {
                        layer.time = 0.0f;
                    }

                    layer.duration = clip.length;
                    layer.clip = clip;

                    //This is a layer that has focus so we need to chose and play a new clip from the pool
                    if (newActiveLayer < i)
                    {
                        //This is the active layer index
                        newActiveLayer = i;
                        //We need to issue a play command to the audio source
                        refreshAudioSource = true;
                    }
                }
                else
                {
                    //The clip assigned to this layer has finished playing and is not set to loop
                    //so clear the later and reset its status ready for reuse in the future
                    layer.clip = null;
                    layer.collection = null;
                    layer.duration = 0.0f;
                    layer.bank = 0;
                    layer.looping = false;
                    layer.time = 0.0f;
                }
            }
            else //Else this layer is playing
            {
                //If this is the highest layer then record that....its the clip currently playing
                if (newActiveLayer < i)
                {
                    newActiveLayer = i;
                }
            }
        }

        //If we found a new active layer (or none)
        if (newActiveLayer != activeLayer || refreshAudioSource)
        {
            //Previous layer expired and no new layer so stop audio source - there are no active layers
            if (newActiveLayer == -1)
            {
                audioSrc.Stop();
                audioSrc.clip = null;
            }
            else //We have a new valid active layer
            {
                AudioLayer layer = audioLayers[newActiveLayer];

                audioSrc.clip = layer.clip;
                audioSrc.volume = layer.muted ? 0.0f : layer.collection.Volume;
                audioSrc.spatialBlend = layer.collection.SpatialBlend;
                audioSrc.time = layer.time;
                audioSrc.loop = false;
                audioSrc.outputAudioMixerGroup = AudioManager.Instance.GetAudioGroupFromTrackName(layer.collection.AudioGroup);

                audioSrc.Play();
            }
        }

        //Remember the currently active layer for the next update check
        activeLayer = newActiveLayer;
        if (activeLayer != -1 && audioSrc) //Handle muted layer
        {
            AudioLayer audioLayer = audioLayers[activeLayer];
            if(audioLayer != null)
            {
                audioSrc.volume = audioLayer.muted ? 0.0f : audioLayer.collection.Volume;
            }
        }
    }
}
