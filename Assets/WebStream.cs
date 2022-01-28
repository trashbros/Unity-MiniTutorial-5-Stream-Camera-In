using System.Collections;
using System.Collections.Generic;
using System.IO; // For Stream classes
using System.Net; // For webserver connection and parsing
using UnityEngine;

public class WebStream : MonoBehaviour
{
    [HideInInspector]
    public System.Byte[] JpegData; // Stores the bytes you read from your server image
    [HideInInspector]
    public string resolution = "480x360"; // TODO

    private Texture2D texture; // The texture you'll be applying your image data to
    private Stream stream; // The server stream you'll be reading from
    private WebResponse resp; // The response to your website request to connect to the server stream
    public MeshRenderer frame; // The mesh renderer on the Unity object you'll be using to display it. (Just a quad is fine. Drag and drop in the editor)
    public string videoURL = "http://camera.buffalotrace.com/mjpg/video.mjpg"; // URL for the video stream, works for mjpg nominally, can't say for others

    // Runs at initialization
    public void Start()
    {
        GetVideo();
    }

    // Tell the system to disconnect the stream.
    public void StopStream()
    {
        stream.Close();
        resp.Close();
    }

    // Connect to a stream and start reading and displaying image frames.
    public void GetVideo()
    {
        // Initialize your 2D texture
        texture = new Texture2D(2, 2);

        // Call out to the URL
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(videoURL);

        // get response
        resp = req.GetResponse();
        // get response stream
        stream = resp.GetResponseStream();
        // Clear your mesh renderer to make sure it's not transparent anymore.
        frame.material.color = Color.white;
        // Start asynchronously running your image stream
        StartCoroutine(GetFrame());
    }

    // Forever running coroutine that's reading from the stream and displaying out
    public IEnumerator GetFrame()
    {
        // Set a size that matches your image frame size
        System.Byte[] JpegData = new System.Byte[505536];
        while (true)
        {
            // Get the image byte data
            int bytesToRead = FindLength(stream);
            Debug.Log(string.Format("Reading {0} bytes.", bytesToRead));
            if (bytesToRead == -1)
            {
                yield break;
            }

            int leftToRead = bytesToRead;

            // Rip out ya data to read, and keep slapping it into the jpegdata holder
            while (leftToRead > 0)
            {
                Debug.Log("Left To Read" + leftToRead);
                leftToRead -= stream.Read(JpegData, bytesToRead - leftToRead, leftToRead);
                yield return null;
            }

            // Make a memory stream to then slap that jpegdata and load it as a proper image in your texture
            MemoryStream ms = new MemoryStream(JpegData, 0, bytesToRead, false, true);
            texture.LoadImage(ms.GetBuffer());
            //string temp = Application.dataPath + @"/../" + DateTime.Now.ToString() + @".jpg";
            //Debug.Log("Writing camera frame to: " + temp);
            //File.WriteAllBytes(temp, texture.EncodeToJPG());
            // Set your mesh renderer to that texture
            frame.material.mainTexture = texture;
            frame.material.color = Color.white;
            // Clear some empty lines we don't want anymore.
            stream.ReadByte(); // CR after bytes
            stream.ReadByte(); // LF after bytes
        }
    }

    // This is reasonable and also cursed fffff
    int FindLength(Stream stream)
    {
        int b;
        string line = "";
        int result = -1;
        bool atEOL = false;
        while ((b = stream.ReadByte()) != -1)
        {
            if (b == 10) continue; // ignore LF char
            if (b == 13)
            { // CR
                if (atEOL)
                {  // two blank lines means end of header
                    stream.ReadByte(); // eat last LF
                    return result;
                }
                if (line.StartsWith("Content-Length:"))
                {
                    result = System.Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                }
                else
                {
                    line = "";
                }
                atEOL = true;
            }
            else
            {
                atEOL = false;
                line += (char)b;
            }
        }
        return -1;
    }

}
