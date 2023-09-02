using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using WebSocketSharp;

public class CollectableAudio : CollectableItem
{
    //Inspector-Assigned 
    [SerializeField] protected Renderer screenRenderer = null;
    [SerializeField] protected Color emissiveColor = Color.gray;


    //Internal
    protected InventoryItemAudio audioItem = null;


    private void Start()
    {
        base.Start();

        //Cast the inventory item an cache it
        audioItem = inventoryItem as InventoryItemAudio;

        //Set the audio log image in the PDA material
        if(audioItem != null && screenRenderer != null) 
        {
            screenRenderer.material.SetTexture("_EmissionMap", audioItem.Image);
            screenRenderer.material.SetColor("_EmissionColor", emissiveColor);            
        } 
    }


    public override string GetText()
    {
        //Assing the pickup text only once (avoid garbage piling up)
        if (interactiveText.IsNullOrEmpty())
        {
            if (audioItem != null)
            {
                interactiveText = $"Audio Log: {audioItem.Person} \n{audioItem.Subject}\n{audioItem.PickupText}";
            }
            else
            {
                interactiveText = "Audio Log: Empty";
            }
        }

        return interactiveText;
    }

    public override void Activate(CharacterManager _chrManager)
    {
        //This is an empty PDA so nothing to take
        if (audioItem == null)
            return;

        //We need a valid character manager and inventory manager
        if(inventory != null)
        {
            if (inventory.AddItem(this, true))
            {
                //Set Empty Text
                interactiveText = "Audio Log: Empty";

                //Remove Data
                inventoryItem = audioItem = null;

                //Disable Screen Texture
                if(screenRenderer != null)
                {
                    screenRenderer.material.SetTexture("_EmissionMap", null);
                    screenRenderer.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
