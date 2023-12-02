using UnityEngine;


/// <summary>
/// Describes the interface for all AnimatorStateCallback derived classes. Each has a
/// empty default implementation so you only have to implement the one(s) you need in 
/// derived classes
/// </summary>
public class AnimatorStateCallback : MonoBehaviour
{
    public virtual void OnAction(string _context, CharacterManager _chrMgr = null) 
    { }
    
    public virtual void OnAction(int _context,    CharacterManager _chrMgr = null) 
    { }
    
    public virtual void OnAction(float _context,  CharacterManager _chrMgr = null) 
    { }
    
    public virtual void OnAction(Object _context, CharacterManager _chrMgr = null) 
    { }
}
