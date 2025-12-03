// @ts-nocheck

//Global variables
var isMouseOverSummary = false;

//Global Cache variables
var tablesProcesseds = 0;
var alreadyAddedListenerToSearchBar = false;
var pendentSearchJob = undefined;

//Events
window.onscroll = function () {
    FillReadProgressBar();
    ScrollSummaryWithWindow();
    EnableOrDisableGoToTopButton();
    OnScrollSummary(document.getElementById("summary"));
};
window.onload = function () {
    var startBenchmark = window.performance.now();

    FillReadProgressBar();
    ScrollSummaryWithWindow();
    RunPostProcessOfAllToolsTags();
    AsyncCheckForEachSummaryItemVisibility();
    CreateTheSearchMarkers();
    document.title = document.title + " Doc";
    Rainbow.color();
    OnScrollSummary(document.getElementById("summary"));

    //Hide the loading screen
    setTimeout(function () {
        document.getElementById("fullscreenLoadingBlock").setAttribute("style", "opacity: 0.0; pointer-events: none;");
        ScrollSummaryWithWindow();
    }, 500);

    //Get #id of URL (if have) and go to topic automatically
    setTimeout(function () {
        var hash = location.hash.replace(/^#/, "");
        if (hash != "")
            GoToDivSmoothlyCore(document.scrollingElement || document.documentElement, "scrollTop", "", window.pageYOffset, document.getElementById(hash).offsetTop - 80, 500, true);
    }, 700);

    var endBenchmark = window.performance.now();
    document.getElementById("processingTime").innerHTML = "" + (endBenchmark - startBenchmark).toFixed(1);

    //Check by duplicated items on summary or in topics (and mark all summary items that is not connected to any topic)
    MarkAllSummaryItemsAndTopicsThatIsNotConnected();
    CheckIfAllSummaryItemsAndTopicsHaveValidsConnections();
}
window.onresize = function () {
    FillReadProgressBar();
    ScrollSummaryWithWindow();
    OnScrollSummary(document.getElementById("summary"));
}

//Read progress code
function FillReadProgressBar() {
    var winScroll = document.body.scrollTop || document.documentElement.scrollTop;
    var height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    var scrolled = (winScroll / height) * 100;
    document.getElementById("readProgressBar").style.width = scrolled + "%";
}

//Function to make summary search bar works
function CreateTheSearchMarkers() {
    //Create the search markers
    var searchMarkersForSubItems = document.createElement("div");
    searchMarkersForSubItems.setAttribute("id", "summarySearchMarkersForSubItems");
    searchMarkersForSubItems.setAttribute("style", "display: none;");
    document.getElementById("divider").appendChild(searchMarkersForSubItems);

    var searchMarkersForItems = document.createElement("div");
    searchMarkersForItems.setAttribute("id", "summarySearchMarkersForItems");
    searchMarkersForItems.setAttribute("style", "display: none;");
    document.getElementById("divider").appendChild(searchMarkersForItems);
}
function StartSearchJob() {
    //Change the buttons visibility
    document.getElementById("summarySearchStart").style.display = "none";
    document.getElementById("summaryTitleAndSearch").setAttribute("style", "grid-template-columns: auto;");

    //Animate the summary title to search bar
    document.getElementById("summaryTitle").style.width = "0px";
    document.getElementById("summarySearchBar").style.width = "0px";

    //Wait animation delay and change content
    setTimeout(function () {
        //Animate the summary title to search bar
        setTimeout(function () {
            document.getElementById("summarySearchBar").style.width = (document.getElementById("summary").offsetWidth - 36) + "px";
            document.getElementById("summarySearchBar").getElementsByTagName("input")[0].focus();
        }, 50);
        setTimeout(function () {
            document.getElementById("summarySearchEnd").style.display = "flex";
        }, 300);
        document.getElementById("summaryTitle").style.display = "none";
        document.getElementById("summarySearchBar").style.display = "block";
    }, 250);

    //Add the lister to searchbar
    if (alreadyAddedListenerToSearchBar == false)
        document.getElementById("summarySearchBar").getElementsByTagName("input")[0].addEventListener("input", (event) => { DoTheSearchJob(event.target.value.toLowerCase(), true); });

    //Inform that is added a listener to the search bar
    alreadyAddedListenerToSearchBar = true;

    //Do the first call to search job
    document.getElementById("summarySearchMarkersForSubItems").setAttribute("style", "display: block;");
    document.getElementById("summarySearchMarkersForItems").setAttribute("style", "display: block;");
    DoTheSearchJob(document.getElementById("summarySearchBar").getElementsByTagName("input")[0].value.toLocaleLowerCase(), false);
}
function DoTheSearchJob(searchKeywords, waitDelay) {
    //Time to wait
    var delayForSearchMillis = -1;

    //Define the time to wait
    if (waitDelay == true)
        delayForSearchMillis = 1500;
    if (waitDelay == false)
        delayForSearchMillis = 100;

    //If have a pendent search job, cancel it
    if (pendentSearchJob !== undefined)
        clearTimeout(pendentSearchJob);

    //Create a new search task
    pendentSearchJob = setTimeout(function () {
        //Get needed elements
        var allSummarySubItems = document.getElementById("summary").getElementsByTagName("ul")[0].getElementsByClassName("summarySubItem");
        var allSummaryItems = document.getElementById("summary").getElementsByTagName("ul")[0].getElementsByClassName("summaryItem");

        //Prepare the search results
        var searchResults = 0;
        var summarySearchResult = document.getElementById("summarySearchResult");
        var searchResultsForSubItemsPositions = [];
        var searchResultsForItemsPositions = [];
        var summarySearchMarkersForSubItems = document.getElementById("summarySearchMarkersForSubItems");
        var summarySearchMarkersForItems = document.getElementById("summarySearchMarkersForItems");
        var summaryTotalHeight = document.getElementById("summary").getElementsByTagName("ul")[0].offsetHeight;

        //If search keywords is not empty
        if (searchKeywords != "") {
            //First hide all
            for (var i = 0; i < allSummarySubItems.length; i++)
                allSummarySubItems[i].setAttribute("style", "list-style-type: circle; opacity: 0.2;");
            for (var i = 0; i < allSummaryItems.length; i++)
                allSummaryItems[i].parentElement.setAttribute("style", "opacity: 0.2;");

            //Now, show only that matches
            for (var i = 0; i < allSummarySubItems.length; i++)
                if (allSummarySubItems[i].innerHTML.toLowerCase().includes(searchKeywords) == true) {
                    allSummarySubItems[i].setAttribute("style", "list-style-type: circle; opacity: 1.0;");
                    searchResultsForSubItemsPositions.push(allSummarySubItems[i].offsetTop / summaryTotalHeight * 100.0);
                    searchResults += 1;
                }
            for (var i = 0; i < allSummaryItems.length; i++)
                if (allSummaryItems[i].innerHTML.toLowerCase().includes(searchKeywords) == true) {
                    allSummaryItems[i].parentElement.setAttribute("style", "opacity: 1.0;");
                    searchResultsForItemsPositions.push(allSummaryItems[i].parentElement.offsetTop / summaryTotalHeight * 100.0);
                    searchResults += 1;
                }
        }
        //If search keywords is empty
        if (searchKeywords == "") {
            for (var i = 0; i < allSummarySubItems.length; i++)
                allSummarySubItems[i].setAttribute("style", "list-style-type: circle;");
            for (var i = 0; i < allSummaryItems.length; i++)
                allSummaryItems[i].parentElement.removeAttribute("style");
        }

        //Publish the search results in the search markers
        //----- Publish on Search Markers for Summary SubItems -------
        var generatedGradientForSubItems = "linear-gradient(top, transparent 0%,";
        for (var i = 0; i < searchResultsForSubItemsPositions.length; i++) {
            //Get the current percent fixed
            var currentPercent = Math.round(searchResultsForSubItemsPositions[i]);
            if (currentPercent > 100.0)
                currentPercent = 100.0;
            if (currentPercent < 0.0)
                currentPercent = 0.0;

            //Add the current percent to backgroud image
            generatedGradientForSubItems += " transparent " + (currentPercent - 0.5) + "%, red " + (currentPercent - 0.5) + "%, red " + currentPercent + "%, transparent " + currentPercent + "%,";
        }
        generatedGradientForSubItems += " transparent 100%)";
        summarySearchMarkersForSubItems.setAttribute("style", "position: absolute; top: 0px; right: 2px; width: 2px; height: 100%; opacity: 0.8; pointer-events: none; background-image: " + generatedGradientForSubItems + "; background-image: -webkit-" + generatedGradientForSubItems + ";");
        //----- Publish on Search Markers for Summary Items -------
        var generatedGradientForItems = "linear-gradient(top, transparent 0%,";
        for (var i = 0; i < searchResultsForItemsPositions.length; i++) {
            //Get the current percent fixed
            var currentPercent = Math.round(searchResultsForItemsPositions[i]);
            if (currentPercent > 100.0)
                currentPercent = 100.0;
            if (currentPercent < 0.0)
                currentPercent = 0.0;

            //Add the current percent to backgroud image
            generatedGradientForItems += " transparent " + (currentPercent - 0.5) + "%, red " + (currentPercent - 0.5) + "%, red " + currentPercent + "%, transparent " + currentPercent + "%,";
        }
        generatedGradientForItems += " transparent 100%)";
        summarySearchMarkersForItems.setAttribute("style", "position: absolute; top: 0px; right: 2px; width: 2px; height: 100%; opacity: 0.8; pointer-events: none; background-image: " + generatedGradientForItems + "; background-image: -webkit-" + generatedGradientForItems + ";");
        //Publish the search results below search box
        if (searchResults > 0) {
            summarySearchResult.style.opacity = "0.7";
            summarySearchResult.innerHTML = "Found " + searchResults + " results";
        }
        if (searchResults == 0)
            summarySearchResult.style.opacity = "0";

        //Reset the pendent serach job
        pendentSearchJob = undefined;
    }, delayForSearchMillis);

    //Update the UI, while wait for search task execution
    summarySearchResult.innerHTML = (searchKeywords != "") ? "Searching" : "";
    summarySearchResult.style.opacity = "0.7";
}
function FinishSearchJob() {
    //Change the buttons visibility
    document.getElementById("summarySearchEnd").style.display = "none";
    document.getElementById("summaryTitleAndSearch").removeAttribute("style");

    //Animate the summary title to search bar
    setTimeout(function () {
        document.getElementById("summaryTitle").style.width = "0px";
        document.getElementById("summarySearchBar").style.width = "0px";
    }, 50);

    //Wait animation delay and change content
    setTimeout(function () {
        //Enable the finish button
        document.getElementById("summarySearchStart").style.display = "flex";

        //Animate the summary title to search bar
        setTimeout(function () {
            document.getElementById("summaryTitle").style.width = (document.getElementById("summary").offsetWidth - 24 - 48) + "px";
        }, 50);
        document.getElementById("summaryTitle").style.display = "block";
        document.getElementById("summarySearchBar").style.display = "none";
    }, 300);

    //Do the last call to search job, to reset the summary
    DoTheSearchJob("", false);
    document.getElementById("summarySearchMarkersForSubItems").setAttribute("style", "display: none;");
    document.getElementById("summarySearchMarkersForItems").setAttribute("style", "display: none;");
}

//Scroll vertically the summary div code
function ScrollSummaryWithWindow() {
    //Do calcs to know total page height
    var endOfContentPointDiv = document.getElementById("contentEndPoint");
    var topbarHeightSize = 64;

    //Find the divs and calculate the size of each
    var dividerDiv = document.getElementById("divider");
    var dividerHeight = document.body.clientHeight * 0.90;
    dividerDiv.style.height = dividerHeight + "px";
    var summaryDiv = document.getElementById("summary");
    var summaryHeight = document.body.clientHeight * 0.90;
    summaryDiv.style.height = summaryHeight + "px";

    //If is not reached in bottom of page, move the divider and summary with scroll
    if (isElementCurrentlyVisibleInScreen(endOfContentPointDiv) == false) {
        dividerDiv.style.marginTop = "0px";
        summaryDiv.style.marginTop = "0px";
        dividerDiv.style.top = (((document.body.clientHeight - dividerHeight) + (topbarHeightSize * 0.65)) * 0.5) + "px";
        summaryDiv.style.top = (((document.body.clientHeight - dividerHeight) + (topbarHeightSize * 0.65)) * 0.5) + "px";
    }

    //If is reached in bottom of page, put the divider and summary on max bottom
    if (isElementCurrentlyVisibleInScreen(endOfContentPointDiv) == true) {
        var contentDiv = document.getElementById("content");
        dividerDiv.style.marginTop = (contentDiv.offsetHeight - dividerHeight) + "px";
        summaryDiv.style.marginTop = (contentDiv.offsetHeight - summaryHeight) + "px";
        OnScrollSummary(document.getElementById("summary"));
    }

    //Find highlighted summaryitems, and if found one not visible, scroll the summary for the highlighted summary item, only if needed
    var allItemsHighlightedOfSummary = document.getElementsByClassName("summaryItemHighlighted");
    for (var i = 0; i < allItemsHighlightedOfSummary.length; i++)
        ScrollToElementOnlyIfNeeded(allItemsHighlightedOfSummary[i], summaryDiv);
}

//On scroll summary content
function OnScrollSummary(element) {
    //Resize responsively the summary title
    if (document.getElementById("summarySearchStart").style.display != "none")
        document.getElementById("summaryTitle").style.width = (document.getElementById("summary").offsetWidth - 24 - 48) + "px";

    //Position responsively the summary search finish button
    var endSummarySearchBt = document.getElementById("summarySearchEnd");
    var endSummarySearchBtWidth = 12;
    var endSummarySearchBtPosX = (element.offsetWidth - endSummarySearchBtWidth - 24);
    endSummarySearchBt.style.left = endSummarySearchBtPosX + "px";

    //Position the summary scroll indicator
    var scrollIndicator = document.getElementById("summaryScrollIndicador");
    var scrollIndicatorWidth = 24;
    var scrollIndicatorHeight = 24;
    var scrollIndicatorYPos = (document.getElementById("divider").offsetHeight + ((element.offsetHeight - document.getElementById("divider").offsetHeight) * 0.5) - (scrollIndicatorHeight + 4));
    var scrollIndicatorXPos = ((element.offsetWidth / 2.0) + (scrollIndicatorWidth / 2) + (document.getElementById("divider").offsetWidth / 2.0));
    scrollIndicator.setAttribute("style", "margin-top:" + scrollIndicatorYPos + "px; margin-left: -" + scrollIndicatorXPos + "px; height: " + scrollIndicatorHeight + "px; width: " + scrollIndicatorWidth + "px;");
    scrollIndicator.firstElementChild.setAttribute("style", "opacity: 1.0;");

    //If reached the max scroll OR mouse is in summary OR the end of page was reached
    if (element.scrollTop >= (element.scrollHeight - element.offsetHeight - 1) || isMouseOverSummary == true || isElementCurrentlyVisibleInScreen(document.getElementById("contentEndPoint")) == true) {
        scrollIndicator.style.opacity = "0.0";
    }
    else {
        scrollIndicator.style.opacity = "0.8";
    }

    //Avoid the body scroll if the summary already is in max top
    if (element.scrollTop <= 0)
        element.scrollTop = element.scrollTop + 1;
    //Avoid the body scroll if the summary already is in max bottom
    if (element.scrollTop >= (element.scrollHeight - element.offsetHeight))
        element.scrollTop = element.scrollTop - 1;
}
function isMouseOverTheSummary(element, show) {
    isMouseOverSummary = show;
    OnScrollSummary(element);
}

//Code to check if a div is visible and highlight item im summary
function isElementCurrentlyVisibleInScreen(elm) {
    var rect = elm.getBoundingClientRect();
    var viewHeight = Math.max(document.documentElement.clientHeight, window.innerHeight);
    return !(rect.bottom < 0 || rect.top - viewHeight >= 0);
}
function ScrollToElementOnlyIfNeeded(element, container) {
    if (element.offsetTop < container.scrollTop) {
        container.scrollTop = element.offsetTop;
    } else {
        const offsetBottom = element.offsetTop + element.offsetHeight;
        const scrollBottom = container.scrollTop + container.offsetHeight;
        if (offsetBottom > scrollBottom) {
            container.scrollTop = offsetBottom - container.offsetHeight;
        }
    }
}
const DelayBetweenEachSummaryItemCheck = ms => new Promise(res => setTimeout(res, ms));
const AsyncCheckForEachSummaryItemVisibility = async () => {
    while (true) {
        var allItemsOfSummary = document.getElementsByClassName("summaryItem");
        for (var i = 0; i < allItemsOfSummary.length; i++) {
            var currentItem = allItemsOfSummary[i];
            if (currentItem == null)
                continue;
            var correspondentDivValue = currentItem.getAttribute("correspondentTopicId");
            if (correspondentDivValue == null || correspondentDivValue == "")
                continue;
            var correspondentDiv = document.getElementById(correspondentDivValue);
            if (correspondentDiv == null)
                continue;
            if (isElementCurrentlyVisibleInScreen(correspondentDiv) == true)
                currentItem.parentElement.classList.add("summaryItemHighlighted");
            else
                currentItem.parentElement.classList.remove("summaryItemHighlighted");
        }

        await DelayBetweenEachSummaryItemCheck(42);
    }
};
function MarkAllSummaryItemsAndTopicsThatIsNotConnected() {
    //This method will mark all summary items that is not connected to any topic
    var allItemsOfSummary = document.getElementsByClassName("summaryItem");
    for (var i = 0; i < allItemsOfSummary.length; i++) {
        var currentItem = allItemsOfSummary[i];
        if (currentItem == null)
            continue;
        var connectedTopic = document.getElementById(currentItem.getAttribute("correspondentTopicId"));
        if (connectedTopic == null || connectedTopic === undefined) {
            currentItem.parentElement.classList.add("summaryItemNotConnected");
            currentItem.innerHTML = currentItem.innerHTML + "<br>(Topic Not Found In Documentation)";
        }
    }
}
function CheckIfAllSummaryItemsAndTopicsHaveValidsConnections() {
    var validationsDoneWithSuccess = 0;

    //Check if have summary items with duplicated id and notify
    var allItemsOfSummary = document.getElementsByClassName("summaryItem");
    var idsOfExistingSummaryItems = ["-1000"];
    var idsOfExistingDuplicatedSummaryItems = [];
    for (var i = 0; i < allItemsOfSummary.length; i++) {
        var currentItem = allItemsOfSummary[i];
        if (currentItem == null)
            continue;
        var summaryItemId = currentItem.getAttribute("correspondentTopicId");
        if (idsOfExistingSummaryItems.includes(summaryItemId) == true)
            idsOfExistingDuplicatedSummaryItems.push(summaryItemId);
        else
            idsOfExistingSummaryItems.push(summaryItemId);
    }
    if (idsOfExistingDuplicatedSummaryItems.length > 0)
        window.alert("WARNING\n\nThere are one or more Summary Items, with duplicated IDs.\n\nDuplicateds Summary Item IDs [" + idsOfExistingDuplicatedSummaryItems + "]");
    if (idsOfExistingDuplicatedSummaryItems.length == 0) {
        console.log("[Doc] No Summary Items with duplicate IDs were found.");
        validationsDoneWithSuccess += 1;
    }

    //Check if have topics with duplicated id and notify
    var allTopics0 = document.getElementsByTagName("doc.topic");
    var idsOfExistingTopics = ["-1000"];
    var idsOfExistingDuplicatedTopics = [];
    for (var i = 0; i < allTopics0.length; i++) {
        var currentItem = allTopics0[i];
        if (currentItem == null)
            continue;
        var topicId = currentItem.getAttribute("topicid");
        if (topicId == "" || topicId == undefined || topicId == null)
            continue;
        if (idsOfExistingTopics.includes(topicId) == true)
            idsOfExistingDuplicatedTopics.push(topicId);
        else
            idsOfExistingTopics.push(topicId);
    }
    if (idsOfExistingDuplicatedTopics.length > 0)
        window.alert("WARNING\n\nThere are one or more Topics, with duplicated IDs.\n\nDuplicateds Topic IDs [" + idsOfExistingDuplicatedTopics + "]");
    if (idsOfExistingDuplicatedTopics.length == 0) {
        console.log("[Doc] No Topics with duplicate IDs were found.");
        validationsDoneWithSuccess += 1;
    }

    //If all validations are ok, notify
    if (validationsDoneWithSuccess == 2)
        console.log("[Doc] There are no problems with possible duplicates of Topics and Summary Items.");
    if (validationsDoneWithSuccess < 2)
        console.log("[Doc] There are possibles problems with possible duplicates of Topics and Summary Items.");
}

//Function to animate movement to a determined div
function GoToDivSmoothly(linkElement) {
    var correspondentTopicDivId = document.getElementById(linkElement.getAttribute("correspondentTopicId"));
    var correspondentDivOffset = correspondentTopicDivId.offsetTop - 80;
    if (correspondentDivOffset <= 0)
        correspondentDivOffset = 0;
    GoToDivSmoothlyCore(document.scrollingElement || document.documentElement, "scrollTop", "", window.pageYOffset, correspondentDivOffset, 250, true);
}
function GoToDivSmoothlyCore(elem, style, unit, from, to, time, prop) {
    if (!elem) {
        return;
    }
    var start = new Date().getTime(),
        timer = setInterval(function () {
            var step = Math.min(1, (new Date().getTime() - start) / time);
            if (prop) {
                elem[style] = (from + step * (to - from)) + unit;
            } else {
                elem.style[style] = (from + step * (to - from)) + unit;
            }
            if (step === 1) {
                clearInterval(timer);
            }

            ScrollSummaryWithWindow();
            FillReadProgressBar();
        }, 16);
    if (prop) {
        elem[style] = from + unit;
    } else {
        elem.style[style] = from + unit;
    }
}

//Function to enable or disable the goto top button
function EnableOrDisableGoToTopButton() {
    var gotoTopButton = document.getElementById("gotoTopButtonItem");
    if (window.pageYOffset >= (document.documentElement.scrollHeight * 0.05))
        gotoTopButton.style.opacity = "1";
    else
        gotoTopButton.style.opacity = "0";
}
function GoToToButton() {
    GoToDivSmoothlyCore(document.scrollingElement || document.documentElement, "scrollTop", "", window.pageYOffset, 0, 250, true);
}

//Function to show popup with image in full screen
function OpenImageInFullScreen(src) {
    //Get all components
    var fullScreenViewerBg = document.getElementById("fullScreenImageViewerBg");
    var fullScreenViewerPop = document.getElementById("fullScreenImageViewerPop");
    var fullScreenViewerImg = document.getElementById("fullScreenImageViewerImg");
    var fullScreenViewerClose = document.getElementById("fullScreenImageClose");

    //Open window
    fullScreenViewerBg.style.opacity = "0.8";
    fullScreenViewerBg.style.pointerEvents = "all";
    fullScreenViewerPop.style.opacity = "1.0";
    fullScreenViewerImg.style.pointerEvents = "none";
    fullScreenViewerImg.setAttribute("src", src);
    fullScreenViewerClose.style.pointerEvents = "all";
}
function CloseImageFullScreenViewer() {
    //Get all components
    var fullScreenViewerBg = document.getElementById("fullScreenImageViewerBg");
    var fullScreenViewerPop = document.getElementById("fullScreenImageViewerPop");
    var fullScreenViewerImg = document.getElementById("fullScreenImageViewerImg");
    var fullScreenViewerClose = document.getElementById("fullScreenImageClose");

    //Close window
    fullScreenViewerBg.style.opacity = "0.0";
    fullScreenViewerBg.style.pointerEvents = "none";
    fullScreenViewerPop.style.opacity = "0.0";
    fullScreenViewerImg.style.pointerEvents = "none";
    fullScreenViewerClose.style.pointerEvents = "none";
}

//Functions of controls from script code showers
function CopyScriptCodeToClipboard(codeNodeId) {
    //Copy the code to clipboard
    document.getElementById(codeNodeId + "_textarea").select();
    document.execCommand("copy");

    //Alert
    window.alert("The Script Code was copied to your Clipboard!");
}
function ShowExpandedScriptCode(codeNodeId) {
    //Get all components
    var fullScreenViewerBg = document.getElementById("fullScreenCodeViewerBg");
    var fullScreenViewerPop = document.getElementById("fullScreenCodeViewerPop");
    var fullScreenViewerSb = document.getElementById("fullScreenCodeViewerSandbox");
    var fullScreenViewerClose = document.getElementById("fullScreenCodeClose");

    //Get the HTML of script code to show
    var htmlCodeToView = document.getElementById(codeNodeId).getElementsByTagName("pre")[0].innerHTML;

    //Open window
    fullScreenViewerBg.style.opacity = "0.8";
    fullScreenViewerBg.style.pointerEvents = "all";
    fullScreenViewerPop.style.opacity = "1.0";
    fullScreenViewerSb.style.pointerEvents = "all";
    fullScreenViewerSb.innerHTML = "<pre class=\"rainbow-show\" data-trimmed=\"true\">" + htmlCodeToView + "</pre>";
    fullScreenViewerClose.style.pointerEvents = "all";

    //Post format the code in viewer
    fullScreenViewerSb.getElementsByTagName("center")[0].classList.remove("toolTagCodeTitle");
    fullScreenViewerSb.getElementsByTagName("center")[0].innerHTML = "<font style=\"font-weight: bold; font-size: 14px; color: #630000;\">" + fullScreenViewerSb.getElementsByTagName("center")[0].innerHTML + " Code</font>";
}
function CloseExpandedScriptCode() {
    //Get all components
    var fullScreenViewerBg = document.getElementById("fullScreenCodeViewerBg");
    var fullScreenViewerPop = document.getElementById("fullScreenCodeViewerPop");
    var fullScreenViewerSb = document.getElementById("fullScreenCodeViewerSandbox");
    var fullScreenViewerClose = document.getElementById("fullScreenCodeClose");

    //Close window
    fullScreenViewerBg.style.opacity = "0.0";
    fullScreenViewerBg.style.pointerEvents = "none";
    fullScreenViewerPop.style.opacity = "0.0";
    fullScreenViewerSb.style.pointerEvents = "none";
    fullScreenViewerClose.style.pointerEvents = "none";
}

//Function that post process all tools tags
function RenameNode(node, newNodeName) {
    const newNode = node.ownerDocument.createElement(newNodeName);
    Array.from(node.attributes).forEach(attr => newNode.setAttribute(attr.localName, attr.value));
    Array.from(node.childNodes).forEach(childNode => newNode.appendChild(childNode));

    node.parentElement.insertBefore(newNode, node);
    node.parentElement.removeChild(node);

    return newNode;
}
function isInsideCodeTag(element) {
    //Run a recursive code and return TRUE if desired element is inside of a CODE tag
    var currentNode = element.parentElement;
    while (currentNode !== undefined || currentNode != null) {
        if (currentNode === undefined || currentNode == null)
            break;
        if (currentNode.nodeName == "CODE")
            return true;
        currentNode = currentNode.parentElement;
    }
    return false;
}
function RunPostProcessOfAllToolsTags() {
    var temporaryTags;

    //all summary items
    temporaryTags = document.getElementsByClassName("summaryItem");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem.setAttribute("href", "topic-id:" + currentItem.getAttribute("correspondentTopicId"));
    }
    //all li itens that is submenu
    temporaryTags = document.getElementsByClassName("summarySubItem");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem.setAttribute("style", "list-style-type: circle;");
        currentItem.innerHTML = "<font style=\"font-weight: normal;\">" + currentItem.innerHTML + "</font>";
    }

    //topic
    temporaryTags = document.getElementsByTagName("doc.topic");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div id=\"" + currentItem.getAttribute("topicid") + "\">" + content + "</div>";
    }

    //topictitle
    temporaryTags = document.getElementsByTagName("doc.topictitle");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        var topicTitleSeparated = content.split(/:(.*)/s);  //<-- Split only the first ocurrenct of ':' character
        if (topicTitleSeparated.length >= 2) {
            var partOne = topicTitleSeparated[0];
            var partTwo = topicTitleSeparated[1];
            if (partTwo.charAt(0) == " ")             //<-- Remove empty space on start of the string
                partTwo = partTwo.replace(/ /, "");
            currentItem.innerHTML = "<div class=\"toolTagTopicTitle\"><div>" + partOne + "<div class=\"toolTagTopicSeparator\"><img src=\"DocumentationFiles/tools/topic-separator.webp\"/></div>" + partTwo + "</div><a href=\"#" + currentItem.parentElement.parentElement.getAttribute("topicid") + "\">#</a></div>";
        }
        if (topicTitleSeparated.length == 1)
            currentItem.innerHTML = "<div class=\"toolTagTopicTitle\"><div>" + topicTitleSeparated[0] + "</div><a href=\"#" + currentItem.parentElement.parentElement.getAttribute("topicid") + "\">#</a></div>";
    }

    //topicsubtitle
    temporaryTags = document.getElementsByTagName("doc.topicsubtitle");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagTopicSubtitle\"><div class=\"toolTagTopicSubtitleSeparator\"><img src=\"DocumentationFiles/tools/topic-subtitle.webp\"/></div>" + content + "</div>";
    }

    //warn
    temporaryTags = document.getElementsByTagName("doc.warn");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagWarnContainer\"><div class=\"toolTagWarnSubcontainer\"><div class=\"toolTagWarnIcon\"><img src=\"DocumentationFiles/tools/warn.webp\"/></div><div class=\"toolTagWarnContent\"><div class=\"toolTagWarnText\">" + content + "</div></div></div></div>";
    }

    //info
    temporaryTags = document.getElementsByTagName("doc.info");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagInfoContainer\"><div class=\"toolTagInfoSubcontainer\"><div class=\"toolTagInfoIcon\"><img src=\"DocumentationFiles/tools/info.webp\"/></div><div class=\"toolTagInfoContent\"><div class=\"toolTagInfoText\">" + content + "</div></div></div></div>";
    }

    //achiev
    temporaryTags = document.getElementsByTagName("doc.achiev");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagAchievContainer\"><div class=\"toolTagAchievSubcontainer\"><div class=\"toolTagAchievIcon\"><img src=\"DocumentationFiles/tools/achiev.webp\"/></div><div class=\"toolTagAchievContent\"><div class=\"toolTagAchievText\">" + content + "</div></div></div></div>";
    }

    //detach
    temporaryTags = document.getElementsByTagName("doc.detach");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagDetach\">" + content + "</div>";
    }

    //icon
    temporaryTags = document.getElementsByTagName("doc.icon");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem.innerHTML = "<div class=\"toolTagIcon\"><img src=\"" + currentItem.getAttribute("src") + "\" /></div>";
    }

    //image
    temporaryTags = document.getElementsByTagName("doc.image");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagImageContainer\"><div class=\"toolTagImageImg\"><img src=\"" + currentItem.getAttribute("src") + "\" title=\"Click here to see ''" + currentItem.getAttribute("src").split("/").pop() + "'' in Fullscreen.\"  onmouseout=\"this.style.opacity = '1';\" onmouseover=\"this.style.opacity = '0.8';\" onclick=\"OpenImageInFullScreen('" + currentItem.getAttribute("src") + "');\" /></div><div class=\"toolTagImageComment\">" + ((content == "") ? "Representation" : content) + "</div></div>";
    }

    //frame
    temporaryTags = document.getElementsByTagName("doc.frame");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML.replace(/<doc.frame/, "§₢");
        var contentDivided = content.split(/₢(.*)/s);
        var text = contentDivided[0].replaceAll("§", "");
        var content = "<doc.frame" + contentDivided[1];
        currentItem.innerHTML = "<div class=\"toolTagFrame\"><div style=\"float: " + currentItem.getAttribute("contentside") + "; margin-" + (currentItem.getAttribute("contentside") == "left" ? "right" : "left") + ": 22px; margin-top: 5px; margin-bottom: 22px; max-width: 70%;\">" + content + "</div>" + text + "</div>";
    }

    //frameci
    temporaryTags = document.getElementsByTagName("doc.frameci");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagFrameImageContainer\"><div class=\"toolTagFrameImageImg\"><img src=\"" + currentItem.getAttribute("src") + "\" title=\"Click here to see ''" + currentItem.getAttribute("src").split("/").pop() + "'' in Fullscreen.\"  onmouseout=\"this.style.opacity = '1';\" onmouseover=\"this.style.opacity = '0.8';\" onclick=\"OpenImageInFullScreen('" + currentItem.getAttribute("src") + "');\" style=\"max-height: " + currentItem.parentElement.parentElement.parentElement.getAttribute("contentheight") + ";\" /></div><div class=\"toolTagFrameImageComment\">" + ((content == "") ? "Representation" : content) + "</div></div>";
    }

    //framecv
    temporaryTags = document.getElementsByTagName("doc.framecv");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagFrameVideoContainer\"><div class=\"toolTagFrameVideoVideo\"><video poster=\"" + currentItem.getAttribute("thumbnail") + "\" style=\"max-height: " + currentItem.parentElement.parentElement.parentElement.getAttribute("contentheight") + ";\" controls><source src=\"" + currentItem.getAttribute("src") + "\" type=\"video/mp4\">The video could not be displayed in your browser.</video></div><div class=\"toolTagFrameVideoComment\">" + content + "</div></div>";
    }

    //flextext
    temporaryTags = document.getElementsByTagName("doc.flextext");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var newFill = document.createElement("div");
        newFill.setAttribute("style", "display: inline; opacity: 0; pointer-events: none; -webkit-user-select: none; -moz-user-select: none; -ms-user-select: none; user-select: none;");
        newFill.innerHTML = "ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        currentItem.parentNode.replaceChild(newFill, currentItem);
    }

    //video
    temporaryTags = document.getElementsByTagName("doc.video");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div class=\"toolTagVideoContainer\"><div class=\"toolTagVideoVideo\"><video poster=\"" + currentItem.getAttribute("thumbnail") + "\" controls><source src=\"" + currentItem.getAttribute("src") + "\" type=\"video/mp4\">The video could not be displayed in your browser.</video></div><div class=\"toolTagVideoComment\">" + content + "</div></div>";
    }

    //code
    temporaryTags = document.getElementsByTagName("doc.code");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var codeContent = currentItem.firstElementChild.innerHTML;
        if (currentItem.getAttribute("language") != "html" && currentItem.getAttribute("language") != "php" && currentItem.getAttribute("language") != "javascript")
            codeContent = codeContent.replace(/<\/.+?>/g, "");  //<-- remove all closing tags (if the language type is not html, php or javascript)
        codeContent = codeContent.replace(/>/g, "&gt;");    //<-- replace all > by &gt;
        codeContent = codeContent.replace(/</g, "&lt;");    //<-- replace all < by &lt;
        codeContent = codeContent.replace(/^\s+|\s+$/g, "");    //<-- remove all useless line breaks before code text and after code text
        currentItem.firstElementChild.innerHTML = codeContent;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<div id=\"code_" + i + "\" class=\"toolTagCodeContainer\"><div class=\"toolTagCodeTitleBar\"><div></div><div class=\"toolTagCodeComment\">Script Code</div><div class=\"toolTagCodeControls\"><div class=\"toolTagCodeControlButton\"><img src=\"DocumentationFiles/tools/copy.webp\" title=\"Copy code to Clipboard\" onclick=\"CopyScriptCodeToClipboard('code_" + i + "');\"/></div><div class=\"toolTagCodeControlButton\"><img src=\"DocumentationFiles/tools/fullscreen.webp\" title=\"Expand the code\"/ onclick=\"ShowExpandedScriptCode('code_" + i + "');\"></div></div></div><pre class=\"toolTagCodeContent\"><center class=\"toolTagCodeTitle\">" + currentItem.getAttribute("language").toUpperCase().replace("CSHARP", "C#") + "</center>" + content.replace("<code>", "<code data-language=\"" + currentItem.getAttribute("language") + "\">") + "</pre><textarea id=\"code_" + i + "_textarea\" class=\"toolTagCodeTextarea\">" + codeContent + "</textarea></div>";
    }

    //tablec
    temporaryTags = [0, 0];
    while (temporaryTags.length > 0) {
        temporaryTags = document.getElementsByTagName("doc.tablec"); //<- update list of all this tags
        if (temporaryTags.length == 0)
            break;
        var currentItem = temporaryTags[0];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem = RenameNode(currentItem, "tr");
        var content = currentItem.innerHTML;
        var contentSplitted = content.split(/=&gt;/); //=>
        currentItem.innerHTML = "";
        for (var x = 0; x < contentSplitted.length; x++)
            if (contentSplitted[x] != "")
                if (contentSplitted[x].match(/\d+/g) || contentSplitted[x].match(/[a-zA-Z]/g))
                    currentItem.innerHTML += "<th>" + contentSplitted[x] + "</th>";
    }

    //tabler
    temporaryTags = [0, 0];
    while (temporaryTags.length > 0) {
        temporaryTags = document.getElementsByTagName("doc.tabler"); //<- update list of all this tags
        if (temporaryTags.length == 0)
            break;
        var currentItem = temporaryTags[0];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem = RenameNode(currentItem, "tr");
        var content = currentItem.innerHTML;
        var contentSplitted = content.split(/=&gt;/); //=>
        currentItem.innerHTML = "";
        for (var x = 0; x < contentSplitted.length; x++)
            if (contentSplitted[x] != "")
                if (contentSplitted[x].match(/\d+/g) || contentSplitted[x].match(/[a-zA-Z]/g))
                    currentItem.innerHTML += "<td>" + contentSplitted[x] + "</td>";
    }

    //table
    temporaryTags = [0, 0];
    while (temporaryTags.length > 0) {
        temporaryTags = document.getElementsByTagName("doc.table"); //<- update list of all this tags
        if (temporaryTags.length == 0)
            break;
        var currentItem = temporaryTags[0];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        currentItem = RenameNode(currentItem, "table");
        currentItem.setAttribute("doc.table", "");
        currentItem.setAttribute("class", "toolTagTable");
    }

    //tablecw
    temporaryTags = [0, 0];
    while (temporaryTags.length > 0) {
        temporaryTags = document.getElementsByTagName("doc.tablecw"); //<- update list of all this tags
        if (temporaryTags.length == 0)
            break;
        var currentItem = temporaryTags[0];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        var parentElement = currentItem.parentElement;
        currentItem.parentElement.removeChild(currentItem);
        var parentContent = parentElement.innerHTML;
        var widths = content.split(/=&gt;/);
        parentElement.setAttribute("class", "");
        parentElement.classList.add("toolTagTable");
        parentElement.classList.add("table" + tablesProcesseds);
        parentElement.innerHTML = "";
        for (var x = 0; x < widths.length; x++)
            parentElement.innerHTML += "<style>.table" + tablesProcesseds + " th:nth-child(" + (x + 1) + "){ width: " + widths[x] + "; }</style>";
        parentElement.innerHTML += parentContent;
        tablesProcesseds += 1;
    }

    //list
    temporaryTags = document.getElementsByTagName("doc.list");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<ul class=\"toolTagList\" style=\"list-style-type: " + ((currentItem.getAttribute("isnumeric") == "true") ? "decimal" : "disc") + ";\">" + content + "</ul>";
    }

    //listr
    temporaryTags = document.getElementsByTagName("doc.listr");
    for (var i = 0; i < temporaryTags.length; i++) {
        var currentItem = temporaryTags[i];
        if (isInsideCodeTag(currentItem) == true) //<-- Ignore this item, if is inside of CODE tag
            continue;
        var content = currentItem.innerHTML;
        currentItem.innerHTML = "<li>" + content + "</li>";
    }
}