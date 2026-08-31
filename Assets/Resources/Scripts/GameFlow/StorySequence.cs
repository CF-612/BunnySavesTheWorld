using UnityEngine;

/// <summary>Small reusable state object for the existing GameObject-based story pages.</summary>
public sealed class StorySequence
{
    private readonly GameObject[] pages;
    private int currentIndex = -1;

    public StorySequence(GameObject[] pages)
    {
        this.pages = pages;
    }

    public bool HasPages
    {
        get
        {
            if (pages == null)
                return false;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                    return true;
            }

            return false;
        }
    }

    public void ResetAndHide()
    {
        currentIndex = -1;
        if (pages == null)
            return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(false);
        }
    }

    /// <returns>True when another page was shown; false when the sequence is complete.</returns>
    public bool ShowNext()
    {
        if (pages == null)
            return false;

        if (currentIndex >= 0 && currentIndex < pages.Length && pages[currentIndex] != null)
            pages[currentIndex].SetActive(false);

        int nextIndex = currentIndex + 1;
        while (nextIndex < pages.Length && pages[nextIndex] == null)
            nextIndex++;

        if (nextIndex >= pages.Length)
            return false;

        currentIndex = nextIndex;
        pages[currentIndex].SetActive(true);
        return true;
    }
}
