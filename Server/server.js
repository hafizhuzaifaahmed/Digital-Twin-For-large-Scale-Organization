const express = require("express");
const app = express();
const port = 3000;

app.get("/generate-building", (req, res) => {
  const buildingData = {
    numberOfFloors: 5,
    roomsPerFloor: [4, 3, 2, 1, 4],
  };

  res.json(buildingData);
});

// Start the server
app.listen(port, () => {
  console.log(`Server is running on http://localhost:${port}`);
});
