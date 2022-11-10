const gallery = document.getElementById("gallery");
const popup = document.getElementById("popup");
const selectedImage = document.getElementById("selectedImage");
const imagesTotal = 99;

//cria a grid de imagens
for (let i = imagesTotal; i > 0; i--) { 
    const imgUrl = "images/cover_episode-" + i + ".jpg" //define o url da imagem

    checkIfImageExists(imgUrl, (exists) => { //checa se o url existe

        if (exists) { //se existir cria a imagem
            const image = document.createElement("img");
            image.src = imgUrl;
            image.alt = "Praça número " + i;
            image.classList.add("galleryImg");

            //console.log(i);
            image.addEventListener("click", () => {
                //popup
                popup.style.transform = "translateY(0)";
                selectedImage.src = imgUrl;
                selectedImage.alt = "Praça número " + i;
            })

            gallery.appendChild(image);
        } else {
            console.log("falhou" + i)
        }
    })

}

popup.addEventListener("click", () => { //fecha o popup quando clica nele
    popup.style.transform = "translateY(-100%)";
    popup.src = "";
    popup.alt = "";
})

//funcao que confere se um url de imagem funciona - https://codepen.io/kallil-belmonte/pen/KKKRoyx
//só funciona na versão online, localmente dá erro
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