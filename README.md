# Saga Nedir?
Saga, birden fazla bağımsız servisteki transaction'ların sistematik bir şekilde işlenerek veri tutarlılığının sağlanmasını amaçlayan bir tasarım desenidir. İlk olarak 1987 yılında (bknz: [SAGAS](https://www.cs.cornell.edu/andru/cs711/2002fa/reading/sagas.pdf)) başlıklı akademik bir makalede tanımlanmıştır. Bu desenin temel mantığı, bir transaction'ın kullanıcı etkileşimi gibi dış bir tetikleyiciyle başlatılmasının ardından, her bir servis transaction'ının başarı durumuna göre sonraki servisin transaction'ının tetiklenmesi esasına dayanır. Bu sayede, mikroservis mimarisi kullanan bir projede, bir işlem sonucunda tüm servislerdeki veriler uyumlu bir şekilde işlenmiş olur. 

Eğer bir transaction sırasında hata oluşur ya da iş mantığı gereği işlem iptal edilmesi gerekirse, tüm süreç durdurulur ve o ana kadar yapılan işlemler geri alınır **(Compensable Transaction)**. Böylece, tüm süreçlerin geri alınabilmesi ile **Atomicity prensibi** korunmuş olur.

- ***Saga, microservice’ler arasında doğru transaction yönetimi ve veri tutarlılığı sağlayan bir design pattern’dır.*** 

Saga, mikroservisler arasında tutarlı bir transaction yönetimi sağlayan bir tasarım desenidir. Bu desenin uygulanması için iki temel yaklaşım geliştirilmiştir: **Events/Choreography** ve **Command/Orchestration**. Şimdi bu yaklaşımlardan biri olan **Choreography** yaklaşımını inceleyelim.


## Saga – Events/Choreography Implemantasyonu

![image](https://github.com/user-attachments/assets/44693233-c07f-4952-ab39-190201c87edf)

Bu yaklaşımda, mikroservisler arasında merkezi bir kontrol noktası olmaksızın, servislerin birbirleriyle olaylar (event) aracılığıyla haberleşmesi esas alınır. Servisler arasındaki iletişim genellikle asenkron olarak gerçekleşir, ki bu ideal bir tasarımdır. Bu tür bir haberleşme çoğunlukla bir mesaj aracısı **(message broker)** kullanılarak gerçekleştirilir.

Choreography yaklaşımını daha detaylı incelersek, ilk serviste başlayan transaction işlevini bitirdikten sonra sonraki servise haber gönderebilmek için message broker üzerinden bir event fırlatacaktır. Ardından bu servisteki transaction işlevini bitirdikten sonra kendisinden sonraki servisi tetikleyebilmek için yine bir event fırlatacaktır ve bu süreç client’ın istediği işlem bitene kadar silsile halinde işlevdeki son servise kadar devam edecektir. Buradan anlayacağımız her bir servisin kendisinden sonraki servisin tetiklenip tetiklenmeyeceğine dair kararı verdiğini görmekteyiz. Her bir servis yapılan işlev neticesinde kendi sürecine bağlı bir şekilde başarılı ya da başarısız bir karar vermekte ve bu neticeye göre ya kendisinden sonraki servisteki transaction’ın başlamasını sağlayabilmekte ya da tüm transaction’ları geri alabilmektedir. Yani her bir servis bizzat karar verici konumundadır.

- ***Choreography implemantasyonu, distributed transaction’a katılacak olan microservice sayısının  ile 4 arasında olduğu durumlarda tercih edilir. 4’ten fazla servis durumunda Orchestration implemantasyonunu uygulamak daha uygundur..***


Choreography’de her bir servis kuyruğu dinler. Dinlediği event message türüne göre gelen bir message söz konusuysa gerekli işlemlerini gerçekleştirir ve sonuç olarak muhakkak durumu bildiren başarılı yahut başarısız bilgisini kuyruğa event olarak ekler. Ardından diğer servisler bu event’e göre ya işlevlerine devam edecektirler ya da tüm transaction’lar geri alınıp veri tutarlılığını sağlayacaktırlar.

Örnek olarak aşağıdaki görseli incelersek eğer bir e-ticaret uygulamasında Choreography implemantasyonuyla oluşturulan siparişi görmekteyiz.

![image](https://github.com/user-attachments/assets/db7bee71-fa44-4c22-9177-c5cf30f1e5d4)

Bu uygulamada bir siparişi oluşturabilmek için;

1. **Order** serviste alınan POST isteği neticesinde sipariş oluşturulur.

. Order events channel kuyruğuna ‘OrderCreated’ misali bir event gönderilir.

3. **Customer** servisi ise Order events channel‘da ki ‘OrderCreated’ event’ına subscribe olmaktadır. Haliyle ilgili kuyruğa beklenen türde bir event gelirse Customer servis tetiklenecektir.

4. **Customer** servis müşteriye dair gerekli tüm işlemleri yaptıktan sonra eğer işlemler başarılıysa ‘OrderSucceded’ isminde yok eğer değilse ‘CreditLimitExceeded’ isminde Customer evens channel kuyruğuna bir event yayar.

5. Haliyle ilgili kuyruğu dinleyen(subscribe) **Order** servis gelen event’ın türüne göre ya siparişi onaylar ya da reddeder.

Görüldüğü üzere servisler arası uçtan uca(point to point) bir iletişim olmadığı için coupling azalacaktır. Ayrıca transaction yönetimi merkezi olmadığı için performance bottleneck azalacaktır..

Ayrıca Choreography yöntemi, sorumlulukları Saga katılımcı servisleri arasında dağıttığından dolayı tek bir hata noktası olmayacaktır. Bundan dolayı da ekstra bakım gerekmemektedir.

Şimdi Choreography implemantasyonuna daha efektif bir örnek vererek ilerleyelim. Örneğimiz yine bir e-ticaret yazılımının sipariş sürecindeki detaylarını yansıtacaktır;

#### 1. Adım
Kullanıcıdan gelen yeni sipariş isteği neticesinde **Order Service** bu siparişi durum bilgisi **Suspend** olacak şekilde kaydeder. Ardından ödeme işlemlerinin gerçekleştirilebilmesi için ***ORDER_CREATED_EVENT*** isimli event’i fırlatır.

![image](https://github.com/user-attachments/assets/ea544718-2d6c-49be-b347-e16f13fb5c66)

#### . Adım
***ORDER_CREATED_EVENT***‘ine subscribe olan **Payment Service** gerekli ödeme işlemlerini gerçekleştirir ve artık alınan ürünlerin stok bilgilerini güncellemek için ***BILLED_ORDER_EVENT*** isimli event’i fırlatır.

#### 3. Adım
***BILLED_ORDER_EVENT***‘ine subscribe olan **Stock Service** bu event fırlatıldığında devreye girer ve gerekli stok ayarlamalarını gerçekleştirir. Artık herhangi bir problem olmadığı taktirde ürünlerin teslimatı için ***ORDER_PREPARED_EVENT*** isimli event’i fırlatır.

#### 4. Adım
***ORDER_PREPARED_EVENT***‘ine subscribe olan **Order Service** artık bu siparişin başarıyla tamamlandığına kanaat getirmiş olarak siparişin durumunu **Completed** olarak günceller.

Yukarıdaki adımlardan herhangi bir durumda hata meydana geldiği taktirde tüm işlemlerin geri alınması gerekmektedir. İşte bu durumda aşağıdaki senaryo devreye girecektir;

![image](https://github.com/user-attachments/assets/3933beef-9b92-4d5f-8a0b-5b2f07d16eb5)

Distributed transaction süreçlerinde bir işlemi geri almak demek esasında o işlemi telafi etmek ya da tam tersini uygulamak için başka bir işlem yapılması demektir(Compensable Transaction) Dolayısıyla yandaki görselden yola çıkarak üstteki işlem akışının yetersiz stok miktarından dolayı **Stock Service**‘de başarısızlığa uğradığını varsayarak sürecin nasıl işlediğini simüle edelim…

#### 1. Adım
Ödeme işleminden sonra **Stock Service**‘te yeterli stok olmadığı anlaşıldığı taktirde ***PRODUCT_OUT_OF_STOCK_EVENT*** isimli event fırlatılır.

#### 2. Adım
***PRODUCT_OUT_OF_STOCK_EVENT***‘ine subscribe olan **Payment Service** ve **Order Service**‘lerde önceki yapılan tüm işlemlerin tersi uygulanır. Nedir bu tersine işlemler diye sorarsanız eğer; **Payment Service**‘te alınan ödeme kullanıcıya tekrar geri yapılır ve **Order Service**‘te ise sipariş durumu Fail olarak güncellenir.


### Saga – Events/Choreography Implemantasyonunun Dezavantajları Nelerdir?

- Hangi servisin hangi kuyruğu dinlediğini takip etmek zorlaşır. Yeni servis eklemek zor ve kafa karıştırıcı olabilmektedir.

- Birbirlerinin kuyruklarını tükettikleri için servisler arası döngüsel bir bağımlılık riski vardır.

- Bir işlemi simüle etmek için tüm servislerin çalışıyor olması gerektiğinden entegrasyon testi zordur.
