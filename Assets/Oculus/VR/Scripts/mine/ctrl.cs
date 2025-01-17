﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ctrl : MonoBehaviour
{
    public Camera cam;
    public OVRGrabber[] grabber;
    //public static GameObject focal;
    public enum Mode { Animate, Play, Parenting }
    public static Mode mode;
    public static PrimitiveType[] pTypes = new PrimitiveType[] { PrimitiveType.Capsule, PrimitiveType.Cube, PrimitiveType.Cylinder, PrimitiveType.Plane, PrimitiveType.Quad, PrimitiveType.Sphere };
    public static Color[] cols = new Color[] { Color.black, Color.white, Color.red, Color.blue, Color.green, Color.grey, Color.cyan, Color.magenta, Color.yellow, (Color.yellow + Color.red) / 2, new Color(.5f, 0, .5f) };
    public static GameObject sel, grObj;

    public static int frame = 0, lastFrame = 10, pType, col, oSkinCount = 2;
    public static float fps = 12;
    public static int scaleAxis, uiLoc;
    public static bool[] parent = new bool[] { true, true };
    public static string debutInfo;

    float playFrame, lean = .5f;
    int creation = 0, grParent;
    bool[] joyAxisReset = new bool[] { true, true };
    bool[] buttBottomReset = new bool[] { true, true };
    bool[] joyInReset = new bool[] { true, true };
    bool[] joyIndexReset = new bool[] { true, true };
    MotionObject grObjMO => MotionScene.motionObj[grObj.GetComponent<OVRGrabbable>().id];
    MotionObject grSelMO => MotionScene.motionObj[grSel.GetComponent<OVRGrabbable>().id];

    float lineWidth = .005f;
    Color lineCol = Color.red;
    LineRenderer[] lineRend = new LineRenderer[2];
    GameObject grSel;
    Color grLast;

    Vector3[] scaleAxises = new Vector3[] { Vector3.one, Vector3.right, Vector3.up, Vector3.forward };
    Vector2[] axisHold = new Vector2[2];
    Vector2 scaleMaxClamp = new Vector2(.05f, 25f);
    Color mainCol = new Color(1, 0, 0, 1);
    Vector3 startLoc = Vector3.zero;

    void Start()
    {
        sel = new GameObject("SEL");

        // 
        for (int i = 0; i < 2; i++)
        {
            lineRend[i] = AddGrabberLine(grabber[i], lineWidth, lineCol);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        debutInfo = MotionScene.motionObj.Count.ToString();

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            col--;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            col++;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreatePrimitive();
        }

        // reset BG color to white
        cam.backgroundColor = Color.white;

        // 
        if (mode == Mode.Animate)
        {
            ModeAnimate();
        }
        else if (mode == Mode.Play)
        {
            ModePlay();
        }
        else if (mode == Mode.Parenting)
        {
            cam.backgroundColor = Color.grey;
            ModeParenting();
        }
    }

    void CopyFrameData()
    {
        for (int i = 0; i < MotionScene.motionObj.Count; i++)
        {
            MotionScene.motionObj[i].MoveObj(frame - 1);
            MotionScene.motionObj[i].UpdateObjData(frame);
        }
    }

    void ModeParenting()
    {
        //lineRend[0].enabled = false;
        //lineRend[1].enabled = false;

        Vector3[] a = new Vector3[] { grabber[grParent].transform.position, grabber[grParent].transform.position + grabber[grParent].transform.forward + grabber[grParent].transform.up };

        lineRend[grParent].SetPositions(a);

        Ray r = new Ray(lineRend[grParent].transform.position, lineRend[grParent].transform.forward + lineRend[grParent].transform.up);
        RaycastHit hit;

        // 
        if (Physics.Raycast(r, out hit))
        {
            if (hit.transform.tag == "Grab")
            {
                // 
                grSel = hit.collider.gameObject;
                sel.transform.position = grSel.transform.position;

                // 
                if (TouchControls.Index[0] || TouchControls.Index[1])
                {
                    // set the parent
                    grObj.transform.SetParent(grSel.transform.position == grObj.transform.position ? null : grSel.transform);

                    // 
                    for (int i = 0; i < oSkinCount; i++)
                    {
                        grObjMO.oSkinP[i].transform.SetParent(grSelMO.oSkinP[i].transform);
                        grObjMO.oSkinN[i].transform.SetParent(grSelMO.oSkinN[i].transform);
                    }

                    EndParenting();
                }
            }
        }
        else
        {
            sel.transform.position = Vector3.up * 10000;

            // unparent if focused on nothing
            if (TouchControls.Index[0] || TouchControls.Index[1])
            {
                grObj.transform.SetParent(null);

                EndParenting();
            }
        }
    }

    void EndParenting()
    {
        // get a reference to the OVR Grabbable for the id
        OVRGrabbable ovrg = grObj.GetComponent<OVRGrabbable>();
        // Update that object's data
        MotionScene.motionObj[ovrg.id].UpdateObjData(frame);

        // Reset everything
        sel.transform.position = Vector3.up * 10000;
        grObj = null;
        grSel = null;

        // Change it back to Animate mode
        mode = Mode.Animate;
    }

    // 
    void StartParenting(int i, OVRGrabber gr)
    {
        grObj = gr.grabbedObject.gameObject;

        gr.GrabEnd();

        mode = Mode.Parenting;

        grParent = i;
    }

    // 
    LineRenderer AddGrabberLine(OVRGrabber gr, float lineWidth, Color lineCol)
    {
        LineRenderer change = gr.gameObject.AddComponent<LineRenderer>();

        Material newMat = new Material(Shader.Find("Unlit/Texture"));
        change.material = newMat;

        change.startWidth = lineWidth;
        change.endWidth = lineWidth;

        change.startColor = lineCol;
        change.endColor = lineCol;

        return change;
    }

    LineRenderer ChangeGrabberLine(OVRGrabber gr, Color lineCol)
    {
        LineRenderer change = gr.gameObject.GetComponent<LineRenderer>();

        change.startColor = lineCol;
        change.endColor = lineCol;

        return change;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // 
        for (int i = 0; i < 2; i++)
        {
            Gizmos.DrawRay(grabber[i].transform.position, grabber[i].transform.forward + grabber[i].transform.up);
        }
    }

    void ModePlay()
    {
        playFrame += Time.deltaTime * fps;

        frame = Mathf.Clamp(Mathf.RoundToInt(playFrame), 0, lastFrame);

        // 
        if (frame == lastFrame)
        {
            playFrame = 0;
        }

        // 
        for (int i = 0; i < 2; i++)
        {
            // 
            if (buttBottomReset[i] && TouchControls.Bottom[i])
            {
                mode = Mode.Animate;
                buttBottomReset[i] = false;
            }

            // 
            if (!TouchControls.Bottom[i])
            {
                buttBottomReset[i] = true;
            }
        }

        UpdateAllObjs(true);
    }

    void ModeAnimate()
    {
        // 
        for (int i = 0; i < 2; i++)
        {
            if (grabber[i].grabbedObject != null)
            {
                GrabbedObjControls(i);
            }

            // 
            if (grabber[0].grabbedObject == null && grabber[1].grabbedObject == null)
            {
                if (TouchControls.Palm[0] || TouchControls.Palm[1])
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (TouchControls.Palm[j])
                        {
                            HideState(j);
                        }

                        lineRend[j].enabled = TouchControls.Palm[j];
                    }
                }
                else
                {
                    // 
                    RawState(i);
                }

                // 
                if (frame > 0 && TouchControls.Index[1])
                {
                    CopyFrameData();
                }
            }

            ResetButtons(i);
        }
    }

    void HideState(int i)
    {
        cam.backgroundColor = (Color.red + Color.white) / 2;

        Vector3[] a = new Vector3[] { grabber[i].transform.position, grabber[i].transform.position + grabber[i].transform.forward + grabber[i].transform.up };

        lineRend[i].SetPositions(a);

        Ray r = new Ray(lineRend[i].transform.position, lineRend[i].transform.forward + lineRend[i].transform.up);
        RaycastHit hit;

        // 
        if (Physics.Raycast(r, out hit))
        {
            if (hit.transform.tag == "Grab")
            {
                // 
                grSel = hit.collider.gameObject;
                sel.transform.position = grSel.transform.position;

                // 
                if (TouchControls.Top[i])
                {
                    OVRGrabbable gr = grSel.GetComponent<OVRGrabbable>();
                    debutInfo = gr.name;
                    // 
                    MotionScene.motionObj[gr.id].ToggleHide(true);
                }

                // 
                if (TouchControls.Bottom[i])
                {
                    OVRGrabbable gr = grSel.GetComponent<OVRGrabbable>();
                    debutInfo = gr.name;
                    // 
                    MotionScene.motionObj[gr.id].ToggleHide(false);
                }
            }
            else
            {
                sel.transform.position = Vector3.up * 10000;
            }
        }
        else
        {
            sel.transform.position = Vector3.up * 10000;
        }
    }

    void GrabbedObjControls(int i)
    {
        // Scale
        Transform meScale = grabber[i].grabbedObject.transform;
        meScale.localScale += (scaleAxises[scaleAxis] * TouchControls.Joystick[i].y * .01f);

        // if too small, delete
        if (meScale.localScale.x < .05f && meScale.localScale.y < .05f && meScale.localScale.z < .05f)
        {
            //MotionScene.motionObj[grabber[i].grabbedObject.id].Delete();
        }

        // 
        if (joyIndexReset[i] && TouchControls.Index[i])
        {
            scaleAxis++;
            joyIndexReset[i] = false;

            // 
            if (scaleAxis == scaleAxises.Length)
            {
                scaleAxis = 0;
            }
        }

        // Parent ADD UI FOR THIS TO COMMUNICATE WHATS UP
        if (parent[i] && TouchControls.Top[i])
        {
            parent[i] = false;

            StartParenting(i, grabber[i]);
        }

        // 
        if (joyInReset[i] && TouchControls.In[i])
        {
            CreatePrimitive(true, i);
            joyInReset[i] = false;
        }

        // 
        if (!TouchControls.In[i])
        {
            joyInReset[i] = true;
        }
    }

    void ResetButtons(int i)
    {
        // 
        if (!TouchControls.Bottom[i])
        {
            buttBottomReset[i] = true;
        }

        if (!TouchControls.Index[i])
        {
            joyIndexReset[i] = true;
        }

        if (!TouchControls.Top[i])
        {
            parent[i] = true;
        }
    }

    IEnumerator ColorObjs(Renderer a, Renderer b)
    {
        Color currentA = a.material.color;
        Color currentB = b.material.color;

        a.material.color = Color.yellow;
        b.material.color = Color.yellow;

        yield return new WaitForSeconds(.5f);

        a.material.color = currentA;
        b.material.color = currentB;

    }

    void RawState(int i)
    {
        lineRend[i].enabled = false;

        // Create using left index
        if (joyIndexReset[0] && (TouchControls.Index[0]))
        {
            joyIndexReset[i] = false;

            CreatePrimitive();
        }

        // 
        float jlY = TouchControls.Joystick[0].y;

        // 
        if (Mathf.Abs(jlY) > 0)
        {
            if (joyAxisReset[0])
            {
                joyAxisReset[0] = false;
                uiLoc -= (int)Mathf.Sign(jlY);
                uiLoc = Mathf.Clamp(uiLoc, 0, 4);
            }
        }
        else if (jlY == 0)
        {
            joyAxisReset[0] = true;
            axisHold[0].y = 0;
        }

        // Frame manipulation
        float jrX = TouchControls.Joystick[1].x;

        // 
        if (Mathf.Abs(jrX) > 0)
        {
            axisHold[1].x += Time.deltaTime;

            // 
            if (joyAxisReset[1])
            {
                joyAxisReset[1] = false;

                // 
                if (uiLoc == 0)
                {
                    frame += (int)Mathf.Sign(jrX);
                    frame = Mathf.Clamp(frame, 0, lastFrame - 1);
                    UpdateAllObjs(false);
                }
                else if (uiLoc == 1)
                {
                    fps += (int)Mathf.Sign(jrX);
                    fps = Mathf.Clamp(fps, 1, 120);
                }
                else if (uiLoc == 2)
                {
                    lastFrame += (int)Mathf.Sign(jrX);
                    lastFrame = Mathf.Clamp(lastFrame, 1, 1000);
                }
                else if (uiLoc == 3)
                {
                    pType += (int)Mathf.Sign(jrX);
                    pType = Mathf.Clamp(pType, 0, pTypes.Length - 1);
                }
                else if (uiLoc == 4)
                {
                    col += (int)Mathf.Sign(jrX);
                    col = Mathf.Clamp(col, 0, cols.Length - 1);
                }
            }
        }
        else if (jrX == 0)
        {
            joyAxisReset[1] = true;
            axisHold[1].x = 0;
        }

        // 
        if (axisHold[1].x > lean)
        {
            if (uiLoc == 0)
            {
                frame += (int)Mathf.Sign(jrX);
                frame = Mathf.Clamp(frame, 0, lastFrame - 1);
                UpdateAllObjs(false);
            }
            else if (uiLoc == 1)
            {
                fps += (int)Mathf.Sign(jrX);
                fps = Mathf.Clamp(fps, 1, 120);
            }
            else if (uiLoc == 2)
            {
                lastFrame += (int)Mathf.Sign(jrX);
                lastFrame = Mathf.Clamp(lastFrame, 1, 1000);
            }
        }

        // 
        if (buttBottomReset[i] && TouchControls.Bottom[i])
        {
            buttBottomReset[i] = false;
            mode = Mode.Play;
            playFrame = frame;
        }
    }

    // 
    void UpdateAllObjs(bool onion)
    {
        for (int i = 0; i < MotionScene.motionObj.Count; i++)
        {
            MotionScene.motionObj[i].MoveObj(frame, updateOS: true, noOnion: onion);
        }
    }

    // 
    GameObject CreatePrimitive(bool copy = false, int hand = -1)
    {
        GameObject g = null;
        OVRGrabbable grab = null;

        // 
        if (!copy)
        {
            // Instantiate our new obj
            g = GameObject.CreatePrimitive(pTypes[pType]);
            // Put it in front of you, kind of
            g.transform.localPosition = cam.transform.forward * .25f + (Vector3.up) + (Vector3.forward / 2);
            // scale it down for some reason
            g.transform.localScale = Vector3.one * .5f;

            // tag it
            g.tag = "Grab";

            // Remove the physics, unfortunately we still need rigidbody for collisions
            Rigidbody rb = g.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Make the object grabbable
            grab = g.AddComponent<OVRGrabbable>();
            grab.enabled = true;
        }
        else
        {
            g = Instantiate(grabber[hand].grabbedObject.gameObject);
            grab = g.GetComponent<OVRGrabbable>();
        }

        // it needs a creation id (TEMP QUICK & LAZY SYSTEM I THINK)
        grab.id = creation;
        // incriment the id
        creation++;

        // Make a new Motion Object
        MotionObject mO = new MotionObject(g, 1000);

        // Add that Motion Object to the Motion Scene
        MotionScene.motionObj.Add(mO);

        return g;
    }
}