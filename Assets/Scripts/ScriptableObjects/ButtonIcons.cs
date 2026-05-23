using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


[CreateAssetMenu(fileName = "ButtonIcons", menuName = "ScriptableObjects/ButtonIcons")]
public class ButtonIcons : SingletonAsset<ButtonIcons>
{

    public Sprite GetKeyboardSprite(string path)
    {
        Sprite attempt = null;
        keyboardSprites.TryGetValue(path, out attempt);
        return attempt != null ? attempt : null;
    }

    public AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> keyboardSprites;

    public Sprite GetGamepadSprite(string path)
    {
        Sprite attempt = null;
        gamepadSprites.TryGetValue(path, out attempt);
        return attempt != null ? attempt : null;
    }

    public AYellowpaper.SerializedCollections.SerializedDictionary<string, Sprite> gamepadSprites;

    //[SerializeField] 
    //private Sprite
    //    q,w,e,r,t,y,u,i,o,p,a,s,d,f,g,h,j,k,l,z,x,c,v,b,n,m,
    //    comma,period,slash,colon,quote,lbrack,rbrack,backslash,
    //    enter,lShift,rShift,tab,backspace,space,plus,minus,
    //    _1,_2,_3,_4,_5,_6,_7,_8,_9,_0;
    //[SerializeField]
    //private Sprite
    //    south, east, west, north,
    //    Dup, Ddown, Dleft, Dright,
    //    lb, rb, lt, rt, start, select;

}
