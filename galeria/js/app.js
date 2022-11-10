const gallery = document.getElementById("gallery");
const popup = document.getElementById("popup");
const selectedImage = document.getElementById("selectedImage");
const imageIndexes = [1, 2, 3];
const imagesTotal = 99;
const selectedIndex = null;

/* imageIndexes.forEach(i => {
    const image = document.createElement("img");
    image.src = "images/cover_episode-" + i + ".jpg";
    image.alt = "Praça número " + i;
    image.classList.add("galleryImg");

    //console.log(i);
    image.addEventListener("click", () => {
        //popup
        popup.style.transform = "translateY(0)";
        selectedImage.src = "images/cover_episode-" + i + ".jpg";
        selectedImage.alt = "Praça número " + i;
    })

    gallery.appendChild(image);
}) */

for (let i = imagesTotal; i > 0; i--) { 
    const imgUrl = "images/cover_episode-" + i + ".jpg"
    checkIfImageExists(imgUrl, (exists) => {
        if (exists) {
        const image = document.createElement("img");
        image.src = "images/cover_episode-" + i + ".jpg";
        image.alt = "Praça número " + i;
        image.classList.add("galleryImg");

        //console.log(i);
        image.addEventListener("click", () => {
            //popup
            popup.style.transform = "translateY(0)";
            selectedImage.src = "images/cover_episode-" + i + ".jpg";
            selectedImage.alt = "Praça número " + i;
        })

        gallery.appendChild(image);
        } else {
        console.log("falhou" + i)
        }
    })

}

popup.addEventListener("click", () => {
    popup.style.transform = "translateY(-100%)";
    popup.src = "";
    popup.alt = "";
})

function checkIfImageExists(url, callback) {
    const img = new Image();
    img.src = url;

    if (img.complete) {
      callback(true);
    } else {
      img.onload = () => {
        callback(true);
      };
      
      img.onerror = () => {
        callback(false);
      };
    }
  }