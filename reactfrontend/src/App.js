import React, { useState } from "react";
import axios from "axios";

const FileUpload = () => {
    const [file, setFile] = useState(null); // For file selection
    const [uploadStatus, setUploadStatus] = useState(""); // To display upload status
    const [topic, setTopic] = useState(""); // To store the topic entered by the user
    const [searchResult, setSearchResult] = useState(""); // To store the result from OpenAI
    const [expandedResult, setExpandedResult] = useState(""); // To store the expanded result
    const [discussionHistory, setDiscussionHistory] = useState([]); // To store the history of discussions
    const [loadingSearch, setLoadingSearch] = useState(false); // Loading indicator for search
    const [loadingExpand, setLoadingExpand] = useState(false); // Loading indicator for expand

    // Handle file selection
    const handleFileChange = (e) => {
        setFile(e.target.files[0]);
        setUploadStatus(""); // Reset status when a new file is selected
        setSearchResult(""); // Clear search results
        setExpandedResult(""); // Clear expanded results
        setTopic(""); // Clear topic input
        setDiscussionHistory([]); // Reset discussion history
    };

    // Upload the file to the backend
    const handleFileUpload = async () => {
        if (!file) {
            setUploadStatus("Please select a file.");
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        try {
            const response = await axios.post(
                "http://localhost:5206/api/FileAnalyzer/upload", // Backend endpoint
                formData
            );
            setUploadStatus(response.data.message || "File uploaded successfully.");
        } catch (error) {
            console.error("Error uploading file:", error);
            setUploadStatus("Failed to upload file.");
        }
    };

    // Make a search API call to the backend
    const handleSearch = async () => {
        if (!topic) {
            alert("Please enter a topic.");
            return;
        }

        setLoadingSearch(true);

        const formData = new FormData();
        formData.append("topic", topic);

        try {
            const response = await axios.post(
                "http://localhost:5206/api/FileAnalyzer/search", // Backend search endpoint
                formData
            );
            setSearchResult(response.data.result || "No result found.");
            setExpandedResult(""); // Reset expanded result for the new topic
        } catch (error) {
            console.error("Error searching topic:", error);
            setSearchResult("Failed to fetch result.");
        } finally {
            setLoadingSearch(false);
        }
    };

    // Make an expand API call to the backend
    const handleExpand = async () => {
        setLoadingExpand(true);

        try {
            const formData = new FormData();
            formData.append("topic", topic); // Send topic as form-data

            const response = await axios.post(
                "http://localhost:5206/api/FileAnalyzer/expand", // Backend expand endpoint
                formData
            );

            setExpandedResult(response.data.result || "No additional information found.");
        } catch (error) {
            console.error("Error expanding topic:", error);
            setExpandedResult("Failed to fetch expanded result.");
        } finally {
            setLoadingExpand(false);
        }
    };

    // Save the current topic discussion to the history
    const handleNewTopicSearch = () => {
        if (topic && searchResult) {
            const discussionString = `Topic: '${topic}'\nResults from notes: ${searchResult}\nExpanded: ${expandedResult || "N/A"}`;
            setDiscussionHistory((prev) => [...prev, discussionString]);
        }
        setTopic(""); // Clear the topic input
        setSearchResult(""); // Clear search results for the new topic
        setExpandedResult(""); // Clear expanded result for the new topic
    };

    return (
        <div>
            <h1>Upload and Analyze PDF</h1>

            {/* File upload input */}
            <input type="file" onChange={handleFileChange} accept="application/pdf" />
            <button onClick={handleFileUpload}>Upload File</button>
            <p>{uploadStatus}</p>

            {/* Display topic input and search button after file upload */}
            {uploadStatus === "File uploaded successfully." && (
                <div>
                    <h2>Enter a Topic to Search</h2>
                    <input
                        type="text"
                        value={topic}
                        onChange={(e) => setTopic(e.target.value)}
                        placeholder="Enter a topic"
                    />
                    <button onClick={handleSearch} disabled={loadingSearch}>
                        {loadingSearch ? "Searching..." : "Search"}
                    </button>
                </div>
            )}

            {/* Display current search result and Expand Topic button */}
            {searchResult && (
                <div>
                    <h2>Current Topic Discussion</h2>
                    <pre style={{ whiteSpace: "pre-wrap", border: "1px solid #ddd", padding: "10px" }}>
                        Topic: '{topic}' {"\n"}
                        Results from notes: {searchResult} {"\n"}
                        Expanded: {expandedResult || "N/A"}
                    </pre>
                    <button onClick={handleExpand} disabled={loadingExpand}>
                        {loadingExpand ? "Expanding..." : "Expand Using AI"}
                    </button>
                    <button onClick={handleNewTopicSearch}>Search for a New Topic</button>
                </div>
            )}

            {/* Display discussion history */}
            {discussionHistory.length > 0 && (
                <div>
                    <h2>Discussion History</h2>
                    {discussionHistory.map((entry, index) => (
                        <pre
                            key={index}
                            style={{
                                whiteSpace: "pre-wrap",
                                border: "1px solid #ddd",
                                padding: "10px",
                                marginBottom: "10px",
                            }}
                        >
                            {entry}
                        </pre>
                    ))}
                </div>
            )}
        </div>
    );
};

export default FileUpload;
