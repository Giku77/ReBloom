using System;
using UnityEngine;
using UnityEngine.UI;

namespace IndieImpulseAssets
{
    public class FakeLoadingScreen : MonoBehaviour
    {
        public Slider progressBar; // Attach your UI Slider here
        public float fakeLoadDuration = 5.0f; // How long the fake loading takes in seconds
        public Text txt;
        private float progress = 0.0f;

        private void Update()
        {
            progress += Time.deltaTime / fakeLoadDuration;

            int percentage = Mathf.FloorToInt(progress * 100);
            txt.text = $"{percentage}%";  // Display the progress in percentage

            // Optionally, update the progress bar value
            progressBar.value = progress;

            if (progress >= 1.0f)
            {
                enabled = false;
            }
        }
    }
}
